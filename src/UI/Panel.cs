﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace KERBALISM
{
  public sealed class Panel
  {
    public Panel()
    {
      headers = new List<Header>();
      sections = new List<Section>();
      callbacks = new List<Action>();
      win_title = string.Empty;
      min_width = 360.0f;

      frequency = new List<ushort>();
    }

    internal void Update()
    {
      throw new NotImplementedException();
    }

    public void Clear()
    {
      headers.Clear();
      sections.Clear();
      win_title = string.Empty;
      min_width = 360.0f;
    }

    public void SetHeader(string label, string tooltip = "", Action click = null)
    {
      Header h = new Header
      {
        label = label,
        tooltip = tooltip,
        click = click,
        icons = new List<Icon>()
      };
      headers.Add(h);
    }

    public void SetSection(string title, string desc = "", Action left = null, Action right = null)
    {
      Section p = new Section
      {
        title = title,
        desc = desc,
        left = left,
        right = right,
        entries = new List<Entry>()
      };
      sections.Add(p);
    }

    public void SetContent(string label, string value = "", string tooltip = "", Action click = null, Action hover = null, short freq = 0)
    {
      Entry e = new Entry
      {
        label = label,
        value = value,
        tooltip = tooltip,
        freq = freq,
        click = click,
        hover = hover,
        icons = new List<Icon>()
      };
      if (sections.Count > 0) sections[sections.Count - 1].entries.Add(e);
    }

    public void SetIcon(Texture texture, string tooltip = "", Action click = null)
    {
      Icon i = new Icon
      {
        texture = texture,
        tooltip = tooltip,
        click = click
      };
      if (sections.Count > 0)
      {
        Section p = sections[sections.Count - 1];
        p.entries[p.entries.Count - 1].icons.Add(i);
      }
      else if (headers.Count > 0)
      {
        Header h = headers[headers.Count - 1];
        h.icons.Add(i);
      }
    }

    public void Render()
    {
      // headers
      foreach (Header h in headers)
      {
        GUILayout.BeginHorizontal(Styles.entry_container);
        GUILayout.Label(new GUIContent(h.label, h.tooltip), Styles.entry_label);
        if (h.click != null && Lib.IsClicked()) callbacks.Add(h.click);
        foreach (Icon i in h.icons)
        {
          GUILayout.Label(new GUIContent(i.texture, i.tooltip), Styles.right_icon);
          if (i.click != null && Lib.IsClicked()) callbacks.Add(i.click);
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10.0f);
      }

      // sections
      foreach (Section p in sections)
      {
        // section title
        GUILayout.BeginHorizontal(Styles.section_container);
        if (p.left != null)
        {
          GUILayout.Label(Icons.left_arrow, Styles.left_icon);
          if (Lib.IsClicked()) callbacks.Add(p.left);
        }
        GUILayout.Label(p.title, Styles.section_text);
        if (p.right != null)
        {
          GUILayout.Label(Icons.right_arrow, Styles.right_icon);
          if (Lib.IsClicked()) callbacks.Add(p.right);
        }
        GUILayout.EndHorizontal();

        // description
        if (p.desc.Length > 0)
        {
          GUILayout.BeginHorizontal(Styles.desc_container);
          GUILayout.Label(p.desc, Styles.desc);
          GUILayout.EndHorizontal();
        }

        // entries
        foreach (Entry e in p.entries)
        {
          GUILayout.BeginHorizontal(Styles.entry_container);
          GUILayout.Label(new GUIContent(e.label, e.tooltip), Styles.entry_label);
          if (e.hover != null && Lib.IsHover()) callbacks.Add(e.hover);
          GUILayout.Label(new GUIContent(e.value, e.tooltip), Styles.entry_value);
          if (e.click != null && Lib.IsClicked()) callbacks.Add(e.click);
          if (e.hover != null && Lib.IsHover()) callbacks.Add(e.hover);

          for (int i = 0; i < e.icons.Count; i++)
          {
            Icon icon = e.icons[i];

            GUILayout.Label(new GUIContent(icon.texture, icon.tooltip), Styles.right_icon);
            if (icon.click != null && Lib.IsClicked()) callbacks.Add(icon.click);
          }
          GUILayout.EndHorizontal();
        }

        // spacing
        GUILayout.Space(10.0f);
      }

      // call callbacks
      if (Event.current.type == EventType.Repaint)
      {
        foreach (Action func in callbacks) func();
        callbacks.Clear();
      }
    }

    public float Height()
    {
      float h = 0.0f;

      h += (float)headers.Count * 26.0f;

      foreach (Section p in sections)
      {
        h += 18.0f + (float)p.entries.Count * 16.0f + 16.0f;
        if (p.desc.Length > 0)
        {
          h += Styles.desc.CalcHeight(new GUIContent(p.desc), min_width - 20.0f);
        }
      }

      return h;
    }

    // utility: decrement an index, warping around 0
    public void Prev(ref int index, int count) => index = (index == 0 ? count : index) - 1;

    // utility: increment an index, warping around a max
    public void Next(ref int index, int count) => index = (index + 1) % count;

    // utility: toggle a flag
    public void Toggle(ref bool b) => b = !b;

    // merge another panel with this one
    public void Add(Panel p)
    {
      headers.AddRange(p.headers);
      sections.AddRange(p.sections);
    }

    // collapse all sections into one
    public void Collapse(string title)
    {
      if (sections.Count > 0)
      {
        sections[0].title = title;
        for (int i = 1; i < sections.Count; ++i)
        {
          sections[0].entries.AddRange(sections[i].entries);
        }
      }
      while (sections.Count > 1)
      {
        sections.RemoveAt(sections.Count - 1);
      }
    }

    // return true if panel has no sections or titles
    public bool Empty() => sections.Count == 0 && headers.Count == 0;

    // set title metadata
    public void Title(string s) => win_title = s;

    // set width metadata
    // - width never shrink
    public void Width(float w) => min_width = Math.Max(w, min_width);

    // get medata
    public string Title() => win_title;
    public float Width() => min_width;

    sealed class Header
    {
      public string label;
      public string tooltip;
      public Action click;
      public List<Icon> icons;
    }

    sealed class Section
    {
      public string title;
      public string desc;
      public Action left;
      public Action right;
      public List<Entry> entries;
    }

    sealed class Entry
    {
      public string label;
      public string value;
      public string tooltip;
      public short freq = 0;
      public Action click;
      public Action hover;
      public List<Icon> icons;
    }

    sealed class Icon
    {
      public Texture texture;
      public string tooltip;
      public Action click;
    }

    List<Header> headers;     // fat entries to show before the first section
    List<Section> sections;   // set of sections
    List<Action> callbacks;   // functions to call on input events
    string win_title;         // metadata stored in panel
    float min_width;          // metadata stored in panel
    List<ushort> frequency;
  }
}
