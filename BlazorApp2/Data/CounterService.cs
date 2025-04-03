using BlazorApp2.Attributes;
using BlazorApp2.Util;
namespace BlazorApp2.Data;

[Serializable]
public class CounterData
{
    [Persistable]
    private int m_count;
    public int Count => m_count;

    [Persistable]
    private DateTime m_lastIncreaseDateTime;
    public string LastIncreaseDateTime => m_lastIncreaseDateTime.ToLongTimeString();

    public void Increase()
    {
        m_count++;
        m_lastIncreaseDateTime = DateTime.Now;
    }
}

public class CounterService : PersistableClass
{
    [Persistable]
    private CounterData m_counterData = new CounterData();
    public CounterData CounterData => m_counterData;

    public void Increase()
    {
        m_counterData.Increase();
        SaveState();
    }
}