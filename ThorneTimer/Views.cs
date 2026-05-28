namespace ThorneTimer
{
    public class ViewData
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public long ActiveYn { get; set; }
        public string StyleFilter { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int SortOrder { get; set; }
        public int ShowWarning { get; set; }
        public string EmptyBehavior { get; set; }
    }
}
