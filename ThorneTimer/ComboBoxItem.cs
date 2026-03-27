namespace ThorneTimer
{
    class ComboBoxItem
    {
        public string Text { get; set; }
        public long Value { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
