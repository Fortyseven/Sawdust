using System;
using System.Windows.Forms;

// SamplesListBox is a specialised version of ListBox. Before letting a left mouse button event get handled the parent class, it calls OnPreMouseDown(),
// after which s_allowLeftMouseDownEvent may be false. In that case, it will block the mouse down event from being sent. This is used as a workaround
// for ListBoxes in multiextended selection mode not supporting drag and drop correctly.
public class SamplesListBox : ListBox
{
	private const int WM_LBUTTONDOWN = 0x201;

    public event EventHandler PreMouseDown;

	// This value gets modified in PreMouseDown
	public static bool s_allowLeftMouseDownEvent = true;

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case WM_LBUTTONDOWN:
                OnPreMouseDown();
                break;
        }

		if (m.Msg != WM_LBUTTONDOWN || s_allowLeftMouseDownEvent)
	        base.WndProc(ref m);
    }

    protected void OnPreMouseDown()
    {
        if (null != PreMouseDown)
            PreMouseDown(this, new EventArgs());
    }

}
