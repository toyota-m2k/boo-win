using io.github.toyota32k.BooLib.Utils;
using Microsoft.UI.Xaml;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Windows.System;
using static BooWin.model.MediaSorter;
using static BooWin.view.viewmodel.EditorKeyCommand.PlayerCommands;

namespace BooWin.view.viewmodel;

public class EditorKeyCommand {
    private KeyCommandManager Manager = new KeyCommandManager();
    public class PlayerCommands {
        public enum ID {
            PLAY = 1,
            PAUSE,
            TOGGLE_PLAY,
            //MUTE,
            //UNMUTE,
            //TOGGLE_MUTE,
            //CLOSE,

            SEEK_FORWARD_0_2,
            SEEK_FORWARD_1,
            SEEK_FORWARD_5,
            SEEK_FORWARD_10,
            SEEK_BACK_0_2,
            SEEK_BACK_1,
            SEEK_BACK_5,
            SEEK_BACK_10,
            SEEK_HEAD,

//            REPEAT_SEEK_FORWORD_1,
            TOGGLE_REPEAT_SEEK_FORWORD_1,

            //SPEED_SLOW,
            //SPEED_NORMAL,
            //SPEED_HIGH,
            //SPEED_SUPER_HIGH,
            //TOGGLE_SPEED_HIGH,
            //TOGGLE_SPEED_SUPER_HIGH,

            CONTINUOUS_HIGH_SPEED,
            CONTINUOUS_SUPER_HIGH_SPEED,

            //RATING_EXCELLENT,
            //RATING_GOOD,
            //RATING_NORMAL,
            //RATING_BAD,
            //RATING_DREADFUL,
            //RATING_BAD_AND_NEXT,
            //RATING_DREADFUL_AND_NEXT,

            //TOGGLE_STRECH_MODE,

            TRIM_SET_START,
            TRIM_SET_END,
            //TRIM_RESET_START,
            //TRIM_RESET_END,

            CHAPTER_ADD,
            CHAPTER_TOGGLE,
            CHAPTER_SET_BEGIN,
            CHAPTER_SET_COMMIT,
            CHAPTER_SET_CANCEL,

            CHAPTER_UNDO,
            CHAPTER_REDO,

            //TOGGLE_CHAPTER_EDIT_MODE,

            //TOGGLE_FULLSCREEN,
            //TOGGLE_PIN_SLIDER,
            //KICKOUT_MOUSE,

            //MOVIE_NEXT,
            //MOVIE_PREV,
            CHAPTER_NEXT,
            CHAPTER_PREV,
            //CHECKED_SET,

            HELP
        }
    }
    public EditorKeyCommand(EditorWindowViewModel viewModel) {
        Manager.RegisterCommand(
            CMD(ID.PLAY, "Play", "Play the media", viewModel.PlayerModel.PlayCommand),
            CMD(ID.PAUSE, "Pause", "Pause the media", viewModel.PlayerModel.PauseCommand),
            CMD(ID.TOGGLE_PLAY, "Toggle Play/Pause", "Toggle between play and pause", viewModel.PlayerModel.TogglePlay),
            REP_CMD(ID.SEEK_FORWARD_0_2, "Seek Forward 0.2s", "Seek forward by 0.2 seconds", () => viewModel.PlayerModel.SeekRelative(200)),
            REP_CMD(ID.SEEK_FORWARD_1, "Seek Forward 1s", "Seek forward by 1 second", () => viewModel.PlayerModel.SeekRelative(1000)),
            REP_CMD(ID.SEEK_FORWARD_5, "Seek Forward 5s", "Seek forward by 5 seconds", () => viewModel.PlayerModel.SeekRelative(5000)),
            REP_CMD(ID.SEEK_FORWARD_10, "Seek Forward 10s", "Seek forward by 10 seconds", () => viewModel.PlayerModel.SeekRelative(10000)),
            REP_CMD(ID.SEEK_BACK_0_2, "Seek Back 0.2s", "Seek backward by 0.2 seconds", () => viewModel.PlayerModel.SeekRelative(-200)),
            REP_CMD(ID.SEEK_BACK_1, "Seek Back 1s", "Seek backward by 1 second", () => viewModel.PlayerModel.SeekRelative(-1000)),
            REP_CMD(ID.SEEK_BACK_5, "Seek Back 5s", "Seek backward by 5 seconds", () => viewModel.PlayerModel.SeekRelative(-5000)),
            REP_CMD(ID.SEEK_BACK_10, "Seek Back 10s", "Seek backward by 10 seconds", () => viewModel.PlayerModel.SeekRelative(-10000)),
            CMD(ID.SEEK_HEAD, "Seek Head", "Seek to the beginning of the media", () => viewModel.PlayerModel.PlayerSeekToCommand.Execute(0)),

            CNT_CMD(ID.CONTINUOUS_HIGH_SPEED, "High Speed", "", () => viewModel.PlayerModel.PlaySpeedCommand.Execute(2), brk: () => viewModel.PlayerModel.PlaySpeedCommand.Execute(1)),
            CNT_CMD(ID.CONTINUOUS_SUPER_HIGH_SPEED, "Super High Speed", "Play in superr high speed mode during key pressed.", viewModel.PlayerModel.StartSuperHighSpeedPlay, brk: viewModel.PlayerModel.StopSuperHighSpeedPlay),
            CMD(ID.TOGGLE_REPEAT_SEEK_FORWORD_1, "Toggle Super High Speed", "Toggle super high speed mode.", viewModel.PlayerModel.ToggleSuperHighSpeedMode),

            CMD(ID.CHAPTER_NEXT, "Next Chapter", "Go to the next chapter", viewModel.PlayerModel.ChapterModel.NextChapterCommand),
            CMD(ID.CHAPTER_PREV, "Prev Chapter", "Go to the previous chapter", viewModel.PlayerModel.ChapterModel.PrevChapterCommand),
            CMD(ID.CHAPTER_ADD, "Add Chapter", "Add a new chapter", viewModel.PlayerModel.ChapterModel.AddChapterCommand),
            CMD(ID.CHAPTER_TOGGLE, "Toggle Chapter", "Toggle chapter enabled state.", viewModel.PlayerModel.ChapterModel.ToggleSkipChapterCommand),
            CMD(ID.CHAPTER_UNDO, "Undo", "Undo chapter editing", viewModel.PlayerModel.ChapterModel.UndoCommand),
            CMD(ID.CHAPTER_REDO, "Redo", "Redo chapter editing", viewModel.PlayerModel.ChapterModel.RedoCommand),

            CMD(ID.CHAPTER_SET_BEGIN, "Begin Cutting Range", "Begin cutting range from current position", viewModel.PlayerModel.BeginCuttingRange),
            CMD(ID.CHAPTER_SET_COMMIT, "Commit Cutting Range", "Commit cutting range to current position", viewModel.PlayerModel.CommitCuttingRange),
            CMD(ID.CHAPTER_SET_CANCEL, "Cancel Cutting Range", "Cancel cutting range", viewModel.PlayerModel.CancelCuttingRange),

            CMD(ID.TRIM_SET_START, "Cut to Start", "Cut range from curren position to start.", viewModel.PlayerModel.CutToStart),
            CMD(ID.TRIM_SET_END, "Cut to End", "Cut range from curent position to end.", viewModel.PlayerModel.CutToEnd),

            CMD(ID.HELP, "Help", "Show Key Command Help", viewModel.HelpCommand)
            );

        AssignSingleKeyCommand(ID.SEEK_FORWARD_0_2, VirtualKey.Right);
        AssignSingleKeyCommand(ID.SEEK_FORWARD_1, VirtualKey.G);
        AssignControlKeyCommand(ID.SEEK_FORWARD_0_2, VirtualKey.G);
        AssignSingleKeyCommand(ID.CONTINUOUS_SUPER_HIGH_SPEED, VirtualKey.F);
        AssignSingleKeyCommand(ID.TOGGLE_REPEAT_SEEK_FORWORD_1, VirtualKey.R);
        AssignShiftKeyCommand(ID.CONTINUOUS_HIGH_SPEED, VirtualKey.F);
        AssignControlKeyCommand(ID.CONTINUOUS_SUPER_HIGH_SPEED, VirtualKey.F);

        //AssignSingleKeyCommand(ID.CHECKED_SET, VirtualKey.N);
        //AssignSingleKeyCommand(ID.MOVIE_NEXT, VirtualKey.PageDown);
        //AssignSingleKeyCommand(ID.MOVIE_PREV, VirtualKey.PageUp);

        AssignSingleKeyCommand(ID.SEEK_BACK_1, VirtualKey.D);
        AssignControlKeyCommand(ID.SEEK_BACK_0_2, VirtualKey.D);
        AssignSingleKeyCommand(ID.SEEK_BACK_0_2, VirtualKey.Left);
        AssignControlKeyCommand(ID.SEEK_HEAD, VirtualKey.Left);
        AssignSingleKeyCommand(ID.SEEK_HEAD, VirtualKey.Home);

        //AssignSingleKeyCommand(ID.CLOSE, VirtualKey.Escape);
        AssignSingleKeyCommand(ID.CHAPTER_TOGGLE, VirtualKey.V);
        AssignSingleKeyCommand(ID.CHAPTER_ADD, VirtualKey.B);
        
        AssignSingleKeyCommand(ID.CHAPTER_SET_BEGIN, VirtualKey.U);
        AssignSingleKeyCommand(ID.CHAPTER_SET_COMMIT, VirtualKey.I);
        AssignSingleKeyCommand(ID.CHAPTER_SET_CANCEL, VirtualKey.Y);

        AssignSingleKeyCommand(ID.TOGGLE_PLAY, VirtualKey.S);

        //AssignSingleKeyCommand(ID.RATING_EXCELLENT, VirtualKey.D5);
        //AssignSingleKeyCommand(ID.RATING_GOOD, VirtualKey.D4);
        //AssignSingleKeyCommand(ID.RATING_NORMAL, VirtualKey.D3);
        //AssignSingleKeyCommand(ID.RATING_BAD, VirtualKey.D2);
        //AssignSingleKeyCommand(ID.RATING_DREADFUL, VirtualKey.D1);

        AssignSingleKeyCommand(ID.TRIM_SET_START, VirtualKey.J);
        AssignSingleKeyCommand(ID.TRIM_SET_END, VirtualKey.K);
        //AssignControlKeyCommand(ID.TRIM_RESET_START, VirtualKey.J);
        //AssignControlKeyCommand(ID.TRIM_RESET_END, VirtualKey.K);

        //AssignSingleKeyCommand(ID.TOGGLE_CHAPTER_EDIT_MODE, VirtualKey.C);

        AssignControlKeyCommand(ID.CHAPTER_UNDO, VirtualKey.Z);
        AssignControlShiftKeyCommand(ID.CHAPTER_REDO, VirtualKey.Z);
        AssignSingleKeyCommand(ID.HELP, VirtualKey.F1);

    }

    private WeakReference<UIElement>? _owner = null;
    public void Attach(UIElement owner, bool enable = true) {
        _owner = new WeakReference<UIElement>(owner);
        if (enable) {
            Enable();
        }
    }

    public void Enable() {
        if (_owner?.TryGetTarget(out var owner) == true) {
            Manager.Enable(owner);
        }
    }
    public void Disable() {
        Manager.Disable();
    }

    #region Private

    // ボタン押下毎に呼び出される普通のコマンド
    private static UTCommand CMD(ID id, string name, string desc, ICommand fn) {
        return new UTCommand((int)id, name, fn).SetDescription(desc);
    }
    private static UTCommand CMD(ID id, string name, string desc, Action fn) {
        return new UTCommand((int)id, name, fn).SetDescription(desc);
    }

    /**
     * ボタン押下中に有効化される継続性コマンド。
     * ボタンを放したときに、brkアクションが呼ばれる。
     */
    private static UTCommand CNT_CMD(ID id, string name, string desc, ICommand fn, Action brk) {
        return new UTCommand((int)id, name, fn).SetDescription(desc).SetBreakAction(brk).SetBreakAction(brk);
    }
    private static UTCommand CNT_CMD(ID id, string name, string desc, Action fn, Action brk) {
        return new UTCommand((int)id, name, fn).SetDescription(desc).SetBreakAction(brk);
    }


    /**
     * ボタン押下中に繰り返して呼ばれるコマンド。
     * 繰り返し回数が必要なら、Action<int>, ReactiveCommand<int> を使用。
     * ボタンを放したときに、brkアクションが呼ばれる。
     */
    private static UTCommand REP_CMD(ID id, string name, string desc, ICommand fn, Action? brk = null) {
        return new UTCommand((int)id, name, fn).SetDescription(desc).SetRepeatable(true);
    }
    //private static UTCommand REP_CMD(ID id, string name, string desc, ReactiveCommand<int> fn, Action? brk = null) {
    //    return new UTCommand((int)id, name, fn).SetDescription(desc).SetRepeatable(true);
    //}
    private static UTCommand REP_CMD(ID id, string name, string desc, Action fn, Action? brk = null) {
        return new UTCommand((int)id, name, fn).SetDescription(desc).SetRepeatable(true).SetBreakAction(brk);
    }
    private static UTCommand REP_CMD(ID id, string name, string desc, Action<int> fn, Action? brk = null) {
        return new UTCommand((int)id, name, fn).SetDescription(desc).SetRepeatable(true).SetBreakAction(brk);
    }

    private void AssignSingleKeyCommand(ID id, VirtualKey key) {
        Manager.AssignSingleKeyCommand((int)id, key);
    }
    private void AssignControlKeyCommand(ID id, VirtualKey key) {
        Manager.AssignControlKeyCommand((int)id, key);
    }
    private void AssignShiftKeyCommand(ID id, VirtualKey key) {
        Manager.AssignShiftKeyCommand((int)id, key);
    }
    private void AssignControlShiftKeyCommand(ID id, VirtualKey key) {
        Manager.AssignControlShiftKeyCommand((int)id, key);
    }

    #endregion

}
