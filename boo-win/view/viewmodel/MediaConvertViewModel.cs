using BooWin.DI.contract;
using io.github.toyota32k.BooLib.Media;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace BooWin.view.viewmodel;

public class MediaConvertViewModel {
    private int ResolutionToLength(VideoResolution res) {
        switch(res) {
            case VideoResolution.SD: return 720;
            default:
            case VideoResolution.HD: return 1280;
            case VideoResolution.FHD: return 1920;
            case VideoResolution.QHD: return 2560;
            case VideoResolution.UHD: return 3840;
        }
    }

    private VideoResolution LengthToResolution(int length) {
        if (length <= 720) return VideoResolution.SD;
        if (length <= 1280) return VideoResolution.HD;
        if (length <= 1920) return VideoResolution.FHD;
        if (length <= 2560) return VideoResolution.QHD;
        else return VideoResolution.UHD;
    }

    public ReactiveProperty<bool> UseFFMpeg = new(false);
    public ReactiveProperty<VideoResolution> Resolution = new(VideoResolution.HD);
    public VideoResolution[] ResolutionList { get; } = Enum.GetValues<VideoResolution>();
    public int MaxLengthInPixel => ResolutionToLength(Resolution.Value);
    public ReactiveProperty<int> CRF = new(23);
    public ReadOnlyReactiveProperty<string> CRFHint { get; }
    public ReadOnlyReactiveProperty<Brush> CRFHintBrush { get; }
    public ReactiveProperty<int> MaxFrameRate = new(30);
    public ReactiveProperty<int> MaxBitRate = new(2_000_000);
    public record BitRateItem(int Value) {
        public string Label => $"{Value / 1_000_000.0:0.#} Mbps";
        public override string ToString() => Label;
    }
    public ObservableCollection<BitRateItem> BitRateList { get; } = [
        new(1_000_000), new(2_000_000), new(4_000_000), new(8_000_000), new(16_000_000)
    ];

    private BitRateItem FindOrAddBitRate(int value) {
        var item = BitRateList.FirstOrDefault(it => it.Value == value);
        if (item != null) return item;
        item = new BitRateItem(value);
        int index = 0;
        while (index < BitRateList.Count && BitRateList[index].Value < value) index++;
        BitRateList.Insert(index, item);
        return item;
    }
    public ReactiveProperty<bool> AutoBitRate = new(true);
    public ReactiveProperty<PresetQuality> AutoQuality = new(PresetQuality.Std);
    public BitrateDirective MaxBitRateDirective => new BitrateDirective(AutoBitRate.Value, AutoQuality.Value, MaxBitRate.Value);
    public ReactiveProperty<BitRateItem?> SelectedBitRate = new();
    public ReactiveProperty<VideoCodecOption> CodecOption = new(VideoCodecOption.NoChange);
    public VideoCodecOption[] CodecOptionList { get; } = Enum.GetValues<VideoCodecOption>();

    public ReactiveProperty<bool> ShowEditorPanel { get; } = new(true);
    public ReactiveCommand SaveAsCommand { get; } = new();
    public ReactiveCommand SaveSettingsAsDefaultCommand { get; } = new();
    public ReactiveCommand SetupFFMpegPathCommand { get; } = new();
    public ReactiveCommand HideEditorPanelCommand { get; } = new();
    public ReactiveCommand ShowEditorPanelCommand { get; } = new();
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;

    public MediaConvertViewModel(ISettingsService settingsService, IDialogService dialogService) {
        _settingsService = settingsService;
        _dialogService = dialogService;
        UseFFMpeg.Value = settingsService.UseFFMpeg;
        //MaxLengthInPixel.Value = settingsService.MaxLengthInPixel;
        Resolution.Value = LengthToResolution(settingsService.MaxLengthInPixel);
        CRF.Value = settingsService.CRF;
        MaxFrameRate.Value = settingsService.MaxFrameRate;
        MaxBitRate.Value = settingsService.MaxBitRate;
        CodecOption.Value = settingsService.OutputVideoCodec;

        MaxBitRate.Subscribe(v => SelectedBitRate.Value = FindOrAddBitRate(v));
        SelectedBitRate.Subscribe(item => {
            if (item != null) MaxBitRate.Value = item.Value;
        });

        SaveSettingsAsDefaultCommand.Subscribe(() => {
            settingsService.UseFFMpeg = UseFFMpeg.Value;
            //settingsService.MaxLengthInPixel = MaxLengthInPixel.Value;
            settingsService.MaxLengthInPixel = MaxLengthInPixel;
            settingsService.CRF = CRF.Value;
            settingsService.MaxFrameRate = MaxFrameRate.Value;
            settingsService.OutputVideoCodec = CodecOption.Value;
            settingsService.Save();
        });
        HideEditorPanelCommand.Subscribe(() => ShowEditorPanel.Value = false);
        ShowEditorPanelCommand.Subscribe(() => ShowEditorPanel.Value = true);

        CRFHint = CRF.Select(crf => crf switch { 
            0 => "Lossless",
            <18 => "Extreme",
            <22 => "High",
            <26 => "Practical",
            <30 => "Compact",
            _=> "Dreadful"
        }).ToReadOnlyReactiveProperty<string>();
        CRFHintBrush = CRF.Select(crf => crf switch {
            < 18 => new SolidColorBrush(Colors.Red),
            <22 => new SolidColorBrush(Colors.Blue),
            <26 => new SolidColorBrush(Colors.Green),
            <30 => new SolidColorBrush(Colors.Orange),
            _ => new SolidColorBrush(Colors.Red)
        }).ToReadOnlyReactiveProperty<Brush>();
    }
}
