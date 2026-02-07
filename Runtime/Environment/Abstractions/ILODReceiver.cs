using System;

namespace Rayforge.Core.Environment.Abstractions
{
    public interface ILODReceiver
    {
        bool UpdateLOD(int newLod);
        void SetVisibility(bool visible);
    }
}
