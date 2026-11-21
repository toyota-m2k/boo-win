using io.github.toyota32k.BooLib.Utils;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Services.Maps;
using static BooWin.model.MediaSorter;

namespace BooWin.model;

public class FilterSettings {
    public string? Category { get; set; } = model.Category.All.Name;

    public bool TypeFilterEnabled { get; set; } = false;
    public bool TypeVideoChecked { get; set; } = true;
    public bool TypeAudioChecked { get; set; } = true;
    public bool TypePhotoChecked { get; set; } = true;

    public bool RatingFilterEnabled { get; set; } = false;
    public bool Rating1Checked { get; set; } = true;
    public bool Rating2Checked { get; set; } = true;
    public bool Rating3Checked { get; set; } = true;
    public bool Rating4Checked { get; set; } = true;
    public bool Rating5Checked { get; set; } = true;

    public bool MarkFilterEnabled { get; set; } = false;
    public bool Mark0Checked { get; set; } = true;
    public bool Mark1Checked { get; set; } = true;
    public bool Mark2Checked { get; set; } = true;
    public bool Mark3Checked { get; set; } = true;

    public bool StartDateEnabled { get; set; } = false;
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.MinValue;
    public bool EndDateEnabled { get; set; } = false;
    public DateTimeOffset EndDate { get; set; } = DateTimeOffset.MaxValue;

    //public FilterSettings(
    //    string? category, 
    //    bool typeFilterEnabled, bool typeVideoChecked, bool typeAudioChecked, bool typePhotoChecked,
    //    bool ratingFilterEnabled, bool rating1Checked, bool rating2Checked, bool rating3Checked, bool rating4Checked, bool rating5Checked,
    //    bool markFilterEnabled, bool mark0Checked, bool mark1Checked, bool Mark2Checked, bool mark3Checked,
    //    bool startDateEnabled, DateTimeOffset startDate, bool endDateEnabled, DateTimeOffset endDate) { 
    //    Category = category;

    //    TypeFilterEnabled = typeFilterEnabled;
    //    TypeVideoChecked = typeVideoChecked;
    //    TypeAudioChecked = typeAudioChecked;
    //    TypePhotoChecked = typePhotoChecked;

    //    RatingFilterEnabled = ratingFilterEnabled;
    //    Rating1Checked = rating1Checked;
    //    Rating2Checked = rating2Checked;
    //    Rating3Checked = rating3Checked;
    //    Rating4Checked = rating4Checked;
    //    Rating5Checked = rating5Checked;

    //    MarkFilterEnabled = markFilterEnabled;
    //    Mark0Checked = mark0Checked;
    //    Mark1Checked = mark1Checked;
    //    Mark3Checked = mark3Checked;

    //    StartDateEnabled = startDateEnabled;
    //    StartDate = startDate;
    //    EndDateEnabled = endDateEnabled;
    //    EndDate = endDate;
    //}

    public static FilterSettings Empty = new FilterSettings();
}

public class SorterSettings {
    public bool SortEnabled { get; set; } = false;
    public Key PrimarySortKey { get; set; } = Key.Date;
    public Key SecondarySortKey { get; set; } = Key.Date;
    public bool Descending { get; set; } = true;

    public static SorterSettings Empty = new SorterSettings();
}

public class HostAdditionalInfo {
    public FilterSettings Filter { get; set; } = FilterSettings.Empty;
    public SorterSettings Sorter { get; set; } = SorterSettings.Empty;
    public SlideShowParams SlideShow { get; set; } = new SlideShowParams();
}

public class HostAdditionalInfoMap {
    public const string SETTING_KEY = "HAIM3";
    public Dictionary<string, HostAdditionalInfo> Map { get; private set; } = new();

    public HostAdditionalInfoMap(Dictionary<string, HostAdditionalInfo>? map) {
        if (map!=null) {
            this.Map = map;
        }
    }
    private bool IsDirty = false;

    //public HostAdditionalInfo this[string hostAddress] {
    //    get => Get(hostAddress);
    //    set => Put(hostAddress, value);
    //}
    public HostAdditionalInfo this[PeerHost host] {
        get => Get(host.Address);
        set => Put(host.Address, value);
    }

    public void SaveTo(JSONSettngStore store) {
        if (IsDirty) {
            store.Set<Dictionary<string, HostAdditionalInfo>>(SETTING_KEY, Map);
            IsDirty = false;
        }
    }

    public static HostAdditionalInfoMap LoadFrom(JSONSettngStore store) {
        return new HostAdditionalInfoMap(
            store.GetOrNull<Dictionary<string, HostAdditionalInfo>>(SETTING_KEY));
    }

    public HostAdditionalInfo Get(string hostAddress) {
        if (!Map.TryGetValue(hostAddress, out var info)) {
            info = new HostAdditionalInfo();
            Map[hostAddress] = info;
        }
        return info;
    }

    public void Put(string hostAddress, HostAdditionalInfo info) {
        Map[hostAddress] = info;
        IsDirty = true;
    }

    public void OnHostListChanged(PeerHostStatusList hostList) {
        var newMap = new Dictionary<string, HostAdditionalInfo>();
        foreach (var host in hostList.Hosts) {
            if (host != null && Map.TryGetValue(host.Address, out var info)) {
                newMap[host.Address] = info;
            }
        }
        Map = newMap;
        IsDirty = true;
    }
}
