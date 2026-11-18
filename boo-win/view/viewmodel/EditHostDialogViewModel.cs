using BooWin.model;
using Reactive.Bindings;
using System.Text.RegularExpressions;

namespace BooWin.view.viewmodel;

public class EditHostDialogViewModel {
    public ReactiveProperty<string> Address { get; } = new ReactiveProperty<string>();
    public ReactiveProperty<string> ServiceName { get; } = new ReactiveProperty<string>();
    public ReactiveProperty<int> Port { get; } = new ReactiveProperty<int>();
    public ReactiveProperty<bool> IsHttps { get; } = new ReactiveProperty<bool>();
    private readonly PeerHost? _src;
    public bool ForEdit => _src != null;
    public string Title => ForEdit ? "Edit Host" : "Add Host";


    public EditHostDialogViewModel(PeerHost? src) {
        _src = src;
        if (src != null) {
            Regex hostport = new Regex(@"(?<addr>[0-9a-zA-Z.]+):(?<port>[0-9]+)");
            var m = hostport.Match(src.Address);
            if (m.Success) {
                Address.Value = m.Groups["addr"].Value;
                Port.Value = int.Parse(m.Groups["port"].Value);
            }
            else {
                Address.Value = src.Address;
                Port.Value = 0;
            }
            ServiceName.Value = src.ServiceName ?? "";
            IsHttps.Value = src.IsHttps;
        }
    }

    public PeerHost GetResult() {
        var addr = Address.Value;
        if (Port.Value > 0) {
            addr += $":{Port.Value}";
        }
        if (_src == null) {
            return new PeerHost(addr, ServiceName.Value, null, IsHttps.Value, null);
        }
        else {
            return new PeerHost(addr, ServiceName.Value, _src.Hostname, _src.IsHttps, _src.Fingerprint);
        }
    }
}
