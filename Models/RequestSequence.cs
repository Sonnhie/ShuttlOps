using System;
using System.Collections.Generic;

namespace ShuttlOps.Models;

public partial class RequestSequence
{
    public DateOnly SeqDate { get; set; }

    public int LastNumber { get; set; }
}
