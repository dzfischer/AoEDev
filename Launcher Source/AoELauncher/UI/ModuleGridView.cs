using System;
using System.Drawing;
using System.Windows.Forms;

namespace AoELauncher.UI;

/// <summary>
/// A plain DataGridView with MultiSelect=true already supports Ctrl-click
/// (toggle) and Shift-click (range) natively and correctly -- the previous
/// version of this app tried to reimplement that logic in a MouseDown event
/// handler and made it worse, because DataGridView's own default selection
/// handling runs as part of its internal OnMouseDown processing, which
/// completes *before* a subscribed public MouseDown event handler ever
/// runs. That ordering is also why "click a row, then drag the whole
/// multi-selection" doesn't work with a plain DataGridView: by the time any
/// event handler gets a chance to intervene, the grid has already collapsed
/// the selection down to just the clicked row.
///
/// The fix is to override the protected OnMouseDown method itself (which
/// runs *before* any of that), and selectively skip calling the base
/// implementation for exactly one case: a plain (no Ctrl/Shift) click on a
/// row that's already part of an existing multi-row selection. In that one
/// case, the selection is left completely untouched (so a drag beginning
/// here still carries the full group), and is only collapsed down to the
/// single clicked row afterward, in OnMouseUp, if no drag actually
/// happened. Every other case (Ctrl-click, Shift-click, clicking an
/// unselected row) falls straight through to the grid's own correct,
/// well-tested default behavior.
/// </summary>
public class ModuleGridView : DataGridView
{
    private Point _dragStart = Point.Empty;
    private bool _dragArmed;
    private bool _deferredSingleSelectPending;
    private int _deferredRowIndex = -1;

    /// <summary>Raised once the mouse has moved far enough (from a valid row, held button) to start a drag-drop gesture.</summary>
    public event EventHandler? DragThresholdReached;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _deferredSingleSelectPending = false;
        _deferredRowIndex = -1;
        _dragArmed = false;

        var hit = HitTest(e.X, e.Y);
        var infoCol = Columns["Info"];
        bool onInfoButton = infoCol != null && hit.ColumnIndex == infoCol.Index;

        if (e.Button == MouseButtons.Left && hit.RowIndex >= 0 && !onInfoButton)
        {
            _dragArmed = true;
            _dragStart = e.Location;

            bool ctrl = (ModifierKeys & Keys.Control) == Keys.Control;
            bool shift = (ModifierKeys & Keys.Shift) == Keys.Shift;

            if (!ctrl && !shift && hit.RowIndex < Rows.Count &&
                Rows[hit.RowIndex].Selected && SelectedRows.Count > 1)
            {
                _deferredSingleSelectPending = true;
                _deferredRowIndex = hit.RowIndex;
                return; // deliberately skip base.OnMouseDown -- don't let the grid collapse the selection yet
            }
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragArmed && e.Button == MouseButtons.Left)
        {
            if (Math.Abs(e.X - _dragStart.X) >= SystemInformation.DragSize.Width ||
                Math.Abs(e.Y - _dragStart.Y) >= SystemInformation.DragSize.Height)
            {
                _dragArmed = false;
                _deferredSingleSelectPending = false; // a drag is happening -- keep the (already-correct) selection intact
                DragThresholdReached?.Invoke(this, EventArgs.Empty);
            }
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_dragArmed && _deferredSingleSelectPending &&
            _deferredRowIndex >= 0 && _deferredRowIndex < Rows.Count)
        {
            // No drag happened after all -- this was just a click, so now collapse to just this row.
            ClearSelection();
            Rows[_deferredRowIndex].Selected = true;
        }

        _dragArmed = false;
        _deferredSingleSelectPending = false;
        _deferredRowIndex = -1;
        base.OnMouseUp(e);
    }
}
