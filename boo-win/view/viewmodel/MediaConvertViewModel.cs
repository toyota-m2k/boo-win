using BooWin.DI.contract;
using io.github.toyota32k.BooLib.Media;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Text;

namespace BooWin.view.viewmodel;

public class MediaConvertViewModel {
    public ReactiveProperty<bool> UseFFMpeg = new(false);
    public ReactiveProperty<int> MaxLengthInPixel = new(1440);
    public ReactiveProperty<int> CRF = new(23);
    public ReactiveProperty<int> MaxFrameRate = new(30);
    public ReactiveProperty<int> MaxBitRate = new(2_000_000);
    public ReactiveProperty<VideoCodecOption> CodecOption = new(VideoCodecOption.NoChange);

    public ReactiveCommand SaveAsCommand = new();
    public ReactiveCommand SaveSettingsAsDefaultCommand = new();
    public ReactiveCommand SetupFFMpegPathCommand = new();
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;

    public MediaConvertViewModel(ISettingsService settingsService, IDialogService dialogService) {
        _settingsService = settingsService;
        _dialogService = dialogService;
        UseFFMpeg.Value = settingsService.UseFFMpeg;
        MaxLengthInPixel.Value = settingsService.MaxLengthInPixel;
        CRF.Value = settingsService.CRF;
        MaxFrameRate.Value = settingsService.MaxFrameRate;
        MaxBitRate.Value = settingsService.MaxBitRate;
        CodecOption.Value = settingsService.OutputVideoCodec;

        SaveSettingsAsDefaultCommand.Subscribe(() => {
            settingsService.UseFFMpeg = UseFFMpeg.Value;
            settingsService.MaxLengthInPixel = MaxLengthInPixel.Value;
            settingsService.CRF = CRF.Value;
            settingsService.MaxFrameRate = MaxFrameRate.Value;
            settingsService.OutputVideoCodec = CodecOption.Value;
            settingsService.Save();
        });
    }
}
