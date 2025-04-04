namespace BlazorApp2.Data
{
    public class KanbanItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
    }

    public class KanbanColumn
    {
        public string ColumnName { get; set; } = "";
        public List<KanbanItem> Items { get; set; } = new List<KanbanItem>();
    }
}
