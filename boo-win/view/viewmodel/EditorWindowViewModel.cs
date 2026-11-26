using BooWin.DI.contract;
using BooWin.DI.implement;
using BooWin.model;
using BooWin.model.editor;
using HttpMultipartParser;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BooWin.view.viewmodel; 
public class EditorWindowViewModel : IItemService {
    private IDialogService _dialogService;
    private IPageService _pageService;
    private ISettingsService _settingsService;


    public ReactivePropertySlim<IEditorItem?> TargetItem { get; } = new ReactivePropertySlim<IEditorItem?>();
    public ReadOnlyReactivePropertySlim<string> WindowTitle { get; }
    public PlayerViewModel PlayerModel { get; private set; }
    public List<Chapter> OrgChapters { get; private set; } = new();

    public EditorWindowViewModel(
        IDialogService dialogService,
        IPageService pageService,
        ISettingsService settingsService
        ) {
        _dialogService = dialogService;
        _pageService = pageService;
        _settingsService = settingsService;
        PlayerModel = new PlayerViewModel(this, dialogService, pageService, settingsService, forEditor: true);
        WindowTitle = TargetItem.Select(item => {
            var name = item?.Name;
            return string.IsNullOrEmpty(name) ? "Boo Editor" : name;
        }).ToReadOnlyReactivePropertySlim<string>();
    }

    public void SetTargetItem(IEditorItem item, List<Chapter>? initialChapters) {
        TargetItem.Value = item;
        OrgChapters = initialChapters ?? new List<Chapter>();
        PlayerModel.SetSource(item);
    }

    public Task<List<Chapter>> LoadChapterAsync(IMediaItem item) {
        return Task.FromResult(OrgChapters);
    }

    public Task<bool> SaveChapterAsync(IMediaItem item, List<Chapter> chapters) {
        return Task.FromResult(true);
    }

    public Uri? GetItemUrl(IMediaItem item, string cmdName = "item") {
        if (item is IEditorItem editorItem) {
            return new Uri(Path.GetFullPath(editorItem.FilePath));
        } else {
            return null;
        }
    }
}
