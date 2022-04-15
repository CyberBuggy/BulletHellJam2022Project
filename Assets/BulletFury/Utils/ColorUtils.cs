using UnityEngine;
using UnityEngine.Rendering;

public static class ColorUtils
{
    public static Color SetColorAlpha(this Color c, float a)
    {
        c.a = a;
        return c;
    }

    public static GUIStyle Style(Color color, bool stretchWidth = false, RectOffset padding = null)
    {
        var currentStyle = new GUIStyle(GUI.skin.box) {border = new RectOffset(-2, -2, -2, -2)};

        var pix = new Color[1];
        pix[0] = color;
        var bg = new Texture2D(1, 1);
        bg.SetPixels(pix);
        bg.Apply();


        currentStyle.normal.background = bg;
        currentStyle.stretchWidth = stretchWidth;
        if (padding != null)
            currentStyle.padding = padding;
        return currentStyle;
    }

    public static GUIStyle Style(Color color, float width, RectOffset padding = null)
    {
        var currentStyle = new GUIStyle(GUI.skin.box) {border = new RectOffset(-2, -2, -2, -2)};

        var pix = new Color[1];
        pix[0] = color;
        var bg = new Texture2D(1, 1);
        bg.SetPixels(pix);
        bg.Apply();

        currentStyle.normal.background = bg;
        currentStyle.fixedWidth = width;
        if (padding != null)
            currentStyle.padding = padding;
        return currentStyle;
    }

    public static GUIContent Content(Color color)
    {
        var pix = new Color[1];
        pix[0] = color;
        var bg = new Texture2D(1, 1);
        bg.SetPixels(pix);
        bg.Apply();
        
        return new GUIContent(bg);
    }
}