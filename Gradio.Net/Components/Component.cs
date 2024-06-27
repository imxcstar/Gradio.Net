﻿using System.Runtime.Serialization;

namespace Gradio.Net;

public abstract class Component : Block
{
    internal Component()
    {

    }

    internal bool? Rtl { get; set; }
    internal string Info { get; set; }
    internal bool? Container { get; set; } = true;
    internal int? Scale { get; set; }
    internal int? MinWidth { get; set; }

    [IgnoreDataMember]
    internal Action LoadFn { get; set; }


    internal object Value { get; set; }
    internal string Label { get; set; }
    internal decimal? Every { get; set; }
    internal bool? ShowLabel { get; set; }


    protected virtual Dictionary<string, object> GetApiInfo()
    {
        return [];
    }
}
