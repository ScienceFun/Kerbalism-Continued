﻿using System;

namespace KERBALISM
{
  public static class UI
  {
    public static void Init()
    {
      // create subsystems
      message = new Message();
      launcher = new Launcher();
      window = new Window(260u, DB.ui.win_left, DB.ui.win_top);
    }

    public static void Sync()
    {
      window.Position(DB.ui.win_left, DB.ui.win_top);
    }

    public static void Update(bool show_window)
    {
      // if gui should be shown
      if (show_window)
      {
        // as a special case, the first time the user enter
        // map-view/tracking-station we open the body info window
        if (MapView.MapIsEnabled && !DB.ui.map_viewed)
        {
          Open(BodyInfo.Body_Info);
          DB.ui.map_viewed = true;
        }

        // update subsystems
        launcher.Update();
        window.Update();

        // remember main window position
        DB.ui.win_left = window.Left();
        DB.ui.win_top = window.Top();
      }

      // re-enable camera mouse scrolling, as some of the on_gui functions can
      // disable it on mouse-hover, but can't re-enable it again consistently
      // (eg: you mouse-hover and then close the window with the cursor still inside it)
      // - we are ignoring user preference on mouse wheel
      GameSettings.AXIS_MOUSEWHEEL.primary.scale = 1.0f;
    }

    public static void OnGUI(bool show_window)
    {
      try
      {
        // render subsystems
        message.OnGUI();
        if (show_window)
        {
          launcher.OnGUI();
          window.OnGUI();
        }
      }
      catch
      {
        Lib.Error("show_window: {0}", show_window);
        Lib.Error("message isNull: {0}", message == null);
        Lib.Error("launcher isNull: {0}", launcher == null);
        Lib.Error("window isNull: {0}", window == null);
      }
    }

    public static void Open(Action<Panel> refresh)
    {
      window.Open(refresh);
    }

    static Message message;
    static Launcher launcher;
    static Window window;
  }
}
