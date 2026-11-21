using System;
using System.Collections.Generic;
using System.Text;

namespace BooWin.model; 

public class SlideShowParams {
    public bool Enabled { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(3);
}
