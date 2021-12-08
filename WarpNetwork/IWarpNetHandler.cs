﻿

namespace WarpNetwork
{
    public interface IWarpNetHandler
    {
        public void Warp();
        public bool GetEnabled();
        public string GetLabel();
        public string GetIconName();
    }
}
