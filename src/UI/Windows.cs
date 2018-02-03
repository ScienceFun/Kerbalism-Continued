using System;
using UnityEngine;

namespace KERBALISM
{
  public sealed class Window
  {
    // - width: window width in pixel
    // - left: initial window horizontal position
    // - top: initial window vertical position
    public Window(uint width, uint left, uint top)
    {
      // generate unique id
      win_id = Lib.RandomInt(int.MaxValue);

      // setup window geometry
      win_rect = new Rect((float)left, (float)top, (float)width, 0.0f);
      
      // setup dragbox geometry
      drag_rect = new Rect(0.0f, 0.0f, width, 20.0f);

      // initialize tooltip utility
      tooltip = new Tooltip();
    }

    public void Open(Action<Panel> refresh)
    {
      this.refresh = refresh;
    }

    public void Close()
    {
      refresh = null;
      panel = null;
    }

    public void Update()
    {
      if (refresh != null)
      {
        // initialize or clear panel
        if (panel == null) panel = new Panel();
        else panel.Clear();

        // refresh panel content
        refresh(panel);

        // if panel is empty, close the window
        if (panel.Empty())
        {
          Close();
        }
      }
    }

    public void OnGUI()
    {
      // window is considered closed if panel is null
      if (panel == null) return;

      // adapt window size to panel
      // - clamp to screen height
      win_rect.width = Math.Min(panel.Width(), Screen.width * 0.75f);
      win_rect.height = Math.Min(20.0f + panel.Height(), Screen.height * 0.75f);

      // clamp the window to the screen, so it can't be dragged outside
      float offset_x = Math.Max(0.0f, -win_rect.xMin) + Math.Min(0.0f, Screen.width - win_rect.xMax);
      float offset_y = Math.Max(0.0f, -win_rect.yMin) + Math.Min(0.0f, Screen.height - win_rect.yMax);
      win_rect.xMin += offset_x;
      win_rect.xMax += offset_x;
      win_rect.yMin += offset_y;
      win_rect.yMax += offset_y;

      // draw the window
      win_rect = GUILayout.Window(win_id, win_rect, Draw_window, "", Styles.win);

      // disable camera mouse scrolling on mouse over
      if (win_rect.Contains(Event.current.mousePosition))
      {
        GameSettings.AXIS_MOUSEWHEEL.primary.scale = 0.0f;
      }
    }

    void Draw_window(int _)
    {
      // render window title
      GUILayout.BeginHorizontal(Styles.title_container);
      GUILayout.Label(Icons.empty, Styles.left_icon);
      GUILayout.Label(panel.Title().ToUpper(), Styles.title_text);
      GUILayout.Label(Icons.close, Styles.right_icon);
      bool b = Lib.IsClicked();
      GUILayout.EndHorizontal();
      if (b) { Close(); return; }

      // start scrolling view
      scroll_pos = GUILayout.BeginScrollView(scroll_pos, HighLogic.Skin.horizontalScrollbar, HighLogic.Skin.verticalScrollbar);

      // render panel content
      panel.Render();

      // end scroll view
      GUILayout.EndScrollView();

      // draw tooltip
      tooltip.Draw(win_rect);

      // right click close the window
      if (Event.current.type == EventType.MouseDown
       && Event.current.button == 1)
      {
        Close();
      }

      // enable dragging
      GUI.DragWindow(drag_rect);
    }

    public void Position(uint x, uint y)
    {
      win_rect.Set((float)x, (float)y, win_rect.width, win_rect.height);
    }

    public uint Left()
    {
      return (uint)win_rect.xMin;
    }

    public uint Top()
    {
      return (uint)win_rect.yMin;
    }

    int win_id;               // store window id
    Rect win_rect;            // store window geometry
    Rect drag_rect;           // store dragbox geometry
    Vector2 scroll_pos;       // used by scroll window mechanics
    Tooltip tooltip;          // tooltip utility
    Panel panel;              // panel
    Action<Panel> refresh;    // refresh function
  }
}
