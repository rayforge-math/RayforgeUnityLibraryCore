namespace Rayforge.Core.Rendering.Blitter
{
    public static class ChannelExtensions
    {
        public static string ToFriendlyString(this Channel channel)
        {
            switch (channel)
            {
                case Channel.R: return "Red";
                case Channel.G: return "Green";
                case Channel.B: return "Blue";
                case Channel.A: return "Alpha";
                default: return "None";
            }
        }
    }
}
