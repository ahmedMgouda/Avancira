﻿namespace Avancira.Admin.Client.Components.EntityTable;

public interface IAddEditModal<TRequest>
{
    TRequest RequestModel { get; }
    bool IsCreate { get; }
    void ForceRender();
}