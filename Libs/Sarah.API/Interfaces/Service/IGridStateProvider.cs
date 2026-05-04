using System;

namespace Sarah.API.Interfaces
{
    public interface IGridStateProvider
    {
        int? CurrentGridState { get; }
        string CurrentGridStateText { get; }
        string CurrentZipCode { get; }
        DateTime? LastChangedAtUtc { get; }
    }
}
