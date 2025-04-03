using BlazorApp2.Attributes;
using System.Reflection;
using System.Text.Json;

namespace BlazorApp2.Util
{
    public class PersistableClass
    {
        private const BindingFlags m_flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

        protected PersistableClass()
        {
            // 생성될 때 자동으로 자신의 상태를 로드
            LoadState();
        }

        /// <summary>
        /// [Persistable] 멤버만 JSON으로 직렬화하여 {클래스명}.json 파일에 저장
        /// </summary>
        public void SaveState()
        {
            // 객체(this)를 재귀적으로 딕셔너리 형태로 변환
            var data = CreateDictionaryFromObject(this);
            if (data == null) return;

            // JSON 직렬화
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);

            string fileName = $"{this.GetType().Name}.json";
            File.WriteAllText(fileName, json);

            Console.WriteLine($"Saved state to {fileName}");
        }

        /// <summary>
        /// JSON 파일({클래스명}.json)로부터 읽어서, [Persistable] 멤버에 값을 적용.
        /// 끝난 뒤, JSON에 존재하지만 실제로 매칭되는 멤버가 없는 leftover 데이터를 제거하고 다시 저장
        /// </summary>
        public void LoadState()
        {
            string fileName = $"{this.GetType().Name}.json";
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"파일 {fileName} 이(가) 존재하지 않아 로드를 생략합니다.");
                return;
            }

            // 최상위 딕셔너리를 역직렬화
            string json = File.ReadAllText(fileName);
            var rootData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (rootData == null)
            {
                Console.WriteLine("로드할 데이터가 없습니다.");
                return;
            }

            // leftover 데이터를 제거하면서 실제 객체에 적용
            ApplyDataToObject(this, rootData);

            // leftover가 제거된 rootData를 다시 파일에 기록
            var options = new JsonSerializerOptions { WriteIndented = true };
            string updatedJson = JsonSerializer.Serialize(rootData, options);
            File.WriteAllText(fileName, updatedJson);

            Console.WriteLine($"'{fileName}'에서 상태를 로드하고 leftover 데이터를 제거했습니다.");
        }

        /// <summary>
        /// 객체의 [Persistable] 멤버들을 재귀적으로 순회하며 Dictionary<string, object> 형태로 만들기
        /// (SaveState에서 사용)
        /// </summary>
        private static object CreateDictionaryFromObject(object obj)
        {
            if (obj == null) return null;

            Type type = obj.GetType();

            // (1) POD 타입 판단 (Primitive, Enum, string, decimal 등)
            if (IsSimpleType(type))
            {
                return obj; // 그대로 반환
            }

            // (2) 컬렉션 등은 상황에 따라 별도 처리할 수 있음
            // 여기서는 예제 단순화를 위해 생략

            // (3) 복합 객체 → 내부 [Persistable] 멤버들을 딕셔너리로 재귀 변환
            var dict = new Dictionary<string, object>();

            // 프로퍼티
            foreach (var prop in type.GetProperties(m_flags))
            {
                if (prop.IsDefined(typeof(PersistableAttribute), true) && prop.CanRead)
                {
                    object value = prop.GetValue(obj);
                    dict[prop.Name] = CreateDictionaryFromObject(value);
                }
            }
            // 필드
            foreach (var field in type.GetFields(m_flags))
            {
                if (field.IsDefined(typeof(PersistableAttribute), true))
                {
                    object value = field.GetValue(obj);
                    dict[field.Name] = CreateDictionaryFromObject(value);
                }
            }

            return dict;
        }

        /// <summary>
        /// rootData에 들어있는 키-값을 객체의 [Persistable] 멤버에 대입(재귀 적용).
        /// leftover 키(더 이상 멤버가 없는 키)는 rootData에서 제거한다.
        /// </summary>
        private static void ApplyDataToObject(object obj, Dictionary<string, JsonElement> data)
        {
            if (obj == null) return;

            var type = obj.GetType();
            // 한 번의 루프에서 data를 수정하면 문제될 수 있으므로, 일단 key들을 수집
            var leftoverKeys = new List<string>();

            // 현재 Dictionary에 있는 key들을 순회
            foreach (var kvp in data)
            {
                string memberName = kvp.Key;
                JsonElement element = kvp.Value;

                // 1) 동일한 이름의 [Persistable] 프로퍼티 찾기
                var prop = type.GetProperty(memberName,
                    m_flags);
                if (prop != null && prop.IsDefined(typeof(PersistableAttribute), true) && prop.CanWrite)
                {
                    // 값 적용
                    object newVal = ConvertJsonElementToValue(element, prop.PropertyType, prop.GetValue(obj));
                    prop.SetValue(obj, newVal);
                    continue;
                }

                // 2) 동일한 이름의 [Persistable] 필드 찾기
                var field = type.GetField(memberName, m_flags);
                if (field != null && field.IsDefined(typeof(PersistableAttribute), true))
                {
                    // 값 적용
                    object newVal = ConvertJsonElementToValue(element, field.FieldType, field.GetValue(obj));
                    field.SetValue(obj, newVal);
                    continue;
                }

                // 3) 아무것도 매칭되지 않았다면 leftover로 처리
                leftoverKeys.Add(memberName);
            }

            // leftover key들은 data에서 제거
            foreach (var leftoverKey in leftoverKeys)
            {
                data.Remove(leftoverKey);
            }
        }

        /// <summary>
        /// JsonElement → 특정 타입으로 역직렬화.
        /// 만약 복합 객체라면, 재귀적으로 [Persistable] 멤버에 데이터를 적용한다.
        /// </summary>
        private static object ConvertJsonElementToValue(JsonElement element, Type targetType, object currentValue)
        {
            // targetType이 POD(기본형, 문자열 등)인지 확인
            if (IsSimpleType(targetType))
            {
                // 단순 변환 시도
                try
                {
                    return element.Deserialize(targetType);
                }
                catch (Exception ex)
                {
                    // 예외 처리: 역직렬화 실패 시, 기존 값을 유지하거나 기본값으로 세팅
                    Console.WriteLine(
                        $"[WARNING] Failed to parse '{targetType.FullName}' from JSON. " +
                        $"Keeping existing value. Error: {ex.Message}");

                    return currentValue;
                    // 필요한 경우, return default; 로 기본값 설정 가능
                }
            }

            // 복합 객체(클래스)라면, 내부 구조는 { key: JsonElement } 형태
            if (element.ValueKind == JsonValueKind.Object)
            {
                // 기존 인스턴스가 없으면 새로 만듦
                if (currentValue == null)
                {
                    currentValue = Activator.CreateInstance(targetType);
                }

                // Json 객체 → Dictionary 로 파싱
                Dictionary<string, JsonElement>? nestedData = null;
                try
                {
                    nestedData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(element.GetRawText());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[WARNING] Failed to parse object for '{targetType.FullName}'. " +
                        $"Error: {ex.Message}");
                    return currentValue;
                }

                if (nestedData != null)
                {
                    // 재귀적으로 [Persistable] 멤버 세팅 + leftover 제거
                    ApplyDataToObject(currentValue, nestedData);
                }
                return currentValue;
            }

            // 그 외(예: 배열, 숫자/문자열이지만 targetType이 다른 경우 등) → 일단 기본 역직렬화 시도
            try
            {
                return element.Deserialize(targetType);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[WARNING] Failed to parse '{targetType.FullName}' from JSON. " +
                    $"Keeping existing value. Error: {ex.Message}");
                return currentValue;
            }
        }

        /// <summary>
        /// int, string, bool, enum, decimal, DateTime 등 기본적으로 직렬화가 쉬운 타입인지 판별하는 헬퍼
        /// </summary>
        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(Guid);
        }
    }
}