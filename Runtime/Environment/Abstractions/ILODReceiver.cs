using System;

namespace Rayforge.Core.Environment.Abstractions
{
    public interface ILODReceiver
    {
        bool UpdateLOD(int newLod, bool useHardDeactivation);
        void SetVisibility(bool visible, bool useHardDeactivation);
    }
}
