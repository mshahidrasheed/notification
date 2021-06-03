//
// Notification form class
//      

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Notification
{
    public enum BackDialogStyle
    {
        None,
        FadedScreen,
        FadedApplication
    }

    public enum Type
    {
        INFO,
        WARNING,
        ERROR,
        OK
    }

    class NoteLocation
    {
        internal int X;
        internal int Y;

        internal Point initialLocation;
        internal bool mouseIsDown = false;

        public NoteLocation(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    public partial class Notification : Form
    {
        static List<Notification> notes = new List<Notification>();     

        private NoteLocation noteLocation;                              
        private short ID                        = 0;                    
        private string description              = "";                   
        private string title                    = "Notifier";           
        private Type type                       = Type.INFO;            

        private bool isDialog = false;                                  
        private BackDialogStyle backDialogStyle = BackDialogStyle.None; 
        private Form myCallerApp;                                       

        private Color Hover = Color.FromArgb(0, 0, 0, 0);               
        private new Color Leave = Color.FromArgb(0, 0, 0, 0);           

        private int timeout_ms                  = 0;                    
        private AutoResetEvent timerResetEvent       = null;            

        private Form inApp = null;                                      

        
        private Notification(string dsc,  Type type, string tit, bool isDialog = false,
                         int timeout_ms = 0, Form insideMe = null)
        {
            this.isDialog      = isDialog;
            this.description   = dsc;
            this.type          = type;
            this.title         = tit;
            this.timeout_ms    = timeout_ms;
            this.inApp         = insideMe;
            
            InitializeComponent();                                      

            foreach (var nt in notes)                                   
                if (nt.ID > ID)
                    ID = nt.ID;
            ID++;                                                       

            if (insideMe != null && !inAppNoteExists())                 
            {
                insideMe.LocationChanged += inApp_LocationChanged;
                insideMe.SizeChanged     += inApp_LocationChanged;
            }

            foreach (Control c in Controls)                                             
            {
                if (c is Label || c.Name == "icon")
                {
                    c.MouseDown += OnMouseDown;
                    c.MouseUp   += OnMouseUp;
                    c.MouseMove += OnMouseMove;
                }
            }
        }
        
        /// <summary>
        /// Handle the drag  drop and resize location of the notes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void inApp_LocationChanged(object sender, EventArgs e)          
        {
            foreach (var note in notes)
            {
                if (note.inApp != null)
                {
                    NoteLocation ln = adjustLocation(note);
                    note.Left       = ln.X;
                    note.Top        = ln.Y;
                }
            }
        }

        /// <summary>
        /// On load event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLoad(object sender, EventArgs e)
        {
            buttonMenu.Hide();

            FormBorderStyle = FormBorderStyle.None;

            this.Tag = "__Notifier|" + ID.ToString("X4");

            setNotifier(description, type, title);  
        }

        /// <summary>
        /// Create the Note and handle its location
        /// </summary>
        /// <param name="description"></param>
        /// <param name="noteType"></param>
        /// <param name="title"></param>
        /// <param name="isUpdate"></param>
        private void setNotifier(string description, Type noteType, string title, bool isUpdate = false)
        {
            this.title          = title;
            this.description    = description;
            this.type           = noteType;

            noteTitle.Text      = title;                                
            noteContent.Text    = description;                    
            noteDate.Text       = DateTime.Now + "";                    
            
            switch (noteType)
            {
                case Type.ERROR:
                    icon.Image = global::Notification.Properties.Resources.ko;
                    Leave = Color.FromArgb(200, 60, 70);
                    Hover = Color.FromArgb(240, 80, 90);
                    break;
                case Type.INFO:
                    icon.Image = global::Notification.Properties.Resources.info;
                    Leave = Color.FromArgb(90, 140, 230);
                    Hover = Color.FromArgb(110, 160, 250);
                    break;
                case Type.WARNING:
                    icon.Image = global::Notification.Properties.Resources.warning;
                    Leave = Color.FromArgb(200, 200, 80);
                    Hover = Color.FromArgb(220, 220, 80);
                    break;
                case Type.OK:
                    icon.Image = global::Notification.Properties.Resources.ok;
                    Leave = Color.FromArgb(80, 200, 130);
                    Hover = Color.FromArgb(80, 240, 130);
                    break;
            }

            buttonClose.BackColor = Leave;
            buttonMenu.BackColor  = Leave;
            noteTitle.BackColor   = Leave;

            // Mouse hover
            this.buttonClose.MouseHover += (s, e) => 
            {
                this.buttonClose.BackColor = Hover;
                this.buttonMenu.BackColor = Hover;
                this.noteTitle.BackColor = Hover;
            };

            this.buttonMenu.MouseHover += (s, e) =>
            {
                this.buttonMenu.BackColor = Hover;
                this.buttonClose.BackColor = Hover;
                this.noteTitle.BackColor = Hover;
            };

            this.noteTitle.MouseHover += (s, e) => 
            {
                this.buttonMenu.BackColor = Hover;
                this.buttonClose.BackColor = Hover;
                this.noteTitle.BackColor = Hover;
            };

            // Mouse leave
            this.buttonClose.MouseLeave += (s, e) =>                    
            {
                this.buttonClose.BackColor = Leave;
                this.buttonMenu.BackColor = Leave;
                this.noteTitle.BackColor = Leave;
            };
            this.buttonMenu.MouseLeave += (s, e) =>
            {
                this.buttonMenu.BackColor = Leave;
                this.buttonClose.BackColor = Leave;
                this.noteTitle.BackColor = Leave;
            };
            this.noteTitle.MouseLeave += (s, e) =>
            {
                this.buttonMenu.BackColor = Leave;
                this.buttonClose.BackColor = Leave;
                this.noteTitle.BackColor = Leave;
            };

           
            if (isDialog)
            {
                Button ok_button    = new Button();                     
                ok_button.FlatStyle = FlatStyle.Flat;
                ok_button.BackColor = Leave;
                ok_button.ForeColor = Color.White;
                Size                = new Size(Size.Width,                  
                                               Size.Height + 50);
                ok_button.Size      = new Size(120, 40);
                ok_button.Location  = new Point(Size.Width / 2 - ok_button.Size.Width / 2, 
                                                Size.Height - 50);
                ok_button.Text      = DialogResult.OK.ToString();
                ok_button.Click     += onOkButtonClick;
                Controls.Add(ok_button);

                noteDate.Location   = new Point(noteDate.Location.X,    // Shift down the date location
                                                noteDate.Location.Y + 44); 


                noteLocation        = new NoteLocation(Left, Top);      // Default Center Location
            }
            
            if (!isDialog && !isUpdate)
            {
                NoteLocation location = adjustLocation(this);           // Set the note location

                Left = location.X;                                      // Notifier position X    
                Top  = location.Y;                                      // Notifier position Y 
            }
        }


        /// <summary>
        /// Find a valid position for the note into the note area:
        /// 1. Inside the Screen (support multiple screens)
        /// 2. Inside the father application (if specified)
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        private NoteLocation adjustLocation(Notification note)
        {
            Rectangle notesArea;
            int nMaxRows    = 0, 
                nColumn     = 0,
                nMaxColumns = 0,
                xShift      = 25;  

            bool add = false;

            if (inApp != null && inApp.WindowState ==  FormWindowState.Normal)
            {
                notesArea = new Rectangle(inApp.Location.X, 
                                          inApp.Location.Y, 
                                          inApp.Size.Width, 
                                          inApp.Size.Height);
            }
            else
            {
                notesArea = new Rectangle(Screen.GetWorkingArea(note).Left,
                                          Screen.GetWorkingArea(note).Top,
                                          Screen.GetWorkingArea(note).Width,
                                          Screen.GetWorkingArea(note).Height);
            }

            nMaxRows    = notesArea.Height / Height;                                  // Max number of rows in the available space
            nMaxColumns = notesArea.Width  / xShift;                                  // Max number of columns in the available space

            noteLocation = new NoteLocation(notesArea.Width  +                        // Initial Position X                                        
                                            notesArea.Left   -
                                            Width,
                                            notesArea.Height +                        // Initial Position Y
                                            notesArea.Top    -
                                            Height);

            while (nMaxRows > 0 && !add)                                              // Check the latest available position (no overlap)
            {
                for (int nRow = 1; nRow <= nMaxRows; nRow++)
                {
                    noteLocation.Y =    notesArea.Height +
                                        notesArea.Top    -
                                        Height * nRow;

                    if (!isLocationAlreadyUsed(noteLocation, note))
                    {
                        add = true; break;
                    }

                    if (nRow == nMaxRows)                                            // X shift if no more column space
                    {
                        nColumn++;
                        nRow = 0;

                        noteLocation.X =  notesArea.Width           +
                                          notesArea.Left            - 
                                          Width - xShift * nColumn;
                    }

                    if (nColumn >= nMaxColumns)                                      // Last exit condition: the screen is full of note
                    {
                        add = true; break;
                    }
                }
            }

            noteLocation.initialLocation = new Point(noteLocation.X,                  // Init the initial Location, for drag & drop
                                                     noteLocation.Y);             
            return noteLocation;
        }
        
        /// <summary>
        /// Close event for the note
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onCloseClick(object sender, EventArgs e)
        {
            if (e == null || ((MouseEventArgs)e).Button != System.Windows.Forms.MouseButtons.Right)
            {
                closeMe();
            }
        }


        /// <summary>
        /// Show the menu (for the menu button) event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onMenuClick(object sender, EventArgs e)
        {
            menu.Show(buttonMenu, new Point(0, buttonMenu.Height));
        }
        
        /// <summary>
        /// Close all the notes event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onMenuCloseAllClick(object sender, EventArgs e)
        {
            CloseAll();
        }


        /// <summary>
        /// Dialog note Only (Ok button click event)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onOkButtonClick(object sender, EventArgs e)          
        {
            onCloseClick(null, null);
        }
        
        /// <summary>
        /// Close the note event
        /// </summary>
        private void closeMe()
        {
            notes.Remove(this);
            this.Close();
      
            if (notes.Count == 0)
                ID = 0;
        }
        
        
        /// <summary>
        /// Check if a note with an inApp capabilities is setted
        /// </summary>
        /// <returns></returns>
        private bool inAppNoteExists()
        {
            foreach (var note in notes)
            {
                if (note.inApp != null)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// check if the specified location (X, Y) is already used by another note
        /// </summary>
        /// <param name="location"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        private bool isLocationAlreadyUsed(NoteLocation location, Notification note)
        {
            foreach (var p in notes)
                if (p.Left == location.X &&
                    p.Top  == location.Y)
                {
                    if (note.inApp != null && 
                        p.ID       == note.ID)
                        return false;
                    return true;
                }
            return false;
        }
        
        /// <summary>
        /// Close all the notes
        /// </summary>
        public static void CloseAll()
        {
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                notes[i].closeMe();
            }
        }
        
        /// <summary>
        /// Event used to draw a right side icon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPaint(object sender, PaintEventArgs e)
        {
            var image = global::Notification.Properties.Resources.close;

            if (image != null)
            {
                var g = e.Graphics;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image,
                    buttonClose.Width  - image.Width,
                    buttonClose.Height - image.Height - 2,
                    image.Width,
                    image.Height);
            }
        }
        
        /// <summary>
        /// Show the note: it is the startup of the creation process of the note
        /// </summary>
        /// <param name="desc"></param>
        /// <param name="type"></param>
        /// <param name="tit"></param>
        /// <param name="isDialog"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static short Show(string desc, Type type = Type.INFO, string tit = "Notifier", bool isDialog  = false, int timeout    = 0)
        {
            short updated_note_id = 0;                                       // If there is already a note with the same content
                                             
            Notification not = new Notification(desc, type, tit, isDialog, timeout, null);
            
            not.Show();                                                         // Show the note
            
            if (not.timeout_ms >= 500)                                          // Start autoclose timer (if any)
            {
                not.timerResetEvent = new AutoResetEvent(false);

                BackgroundWorker timer = new BackgroundWorker();
                timer.DoWork += timer_DoWork;
                timer.RunWorkerCompleted += timer_RunWorkerCompleted;
                timer.RunWorkerAsync(not);                                      // Timer (temporary notes)
            }

            notes.Add(not);                                                     // Add to our collection of Notifiers
            updated_note_id = not.ID;

            return updated_note_id;                                                 // Return the current ID of the created/updated Note
        }

        private static void CloseAllNotifier()
        {
            foreach (var note in notes)
            {
                note.Close();
            }
        }
        
        ///// <summary>
        ///// Check if the note is already present
        ///// Point out the ID and the occurency of the already present note
        ///// </summary>
        ///// <param name="desc"></param>
        ///// <param name="type"></param>
        ///// <param name="tit"></param>
        ///// <param name="isDiag"></param>
        ///// <param name="updated_note_id"></param>
        ///// <param name="updated_note_occurency"></param>
        ///// <returns></returns>
        //private static bool NotifierAlreadyPresent(string desc, Type type, string tit, bool isDiag, out short updated_note_id, out short updated_note_occurency)
        //{
        //    updated_note_id         = 0;
        //    updated_note_occurency  = 0;

        //    foreach (var note in notes)
        //    {
        //        short occurency      = 0;
        //        string filteredTitle = note.title;
        //        int indx             = filteredTitle.IndexOf(']');

        //        if(indx > 0)
        //        {
        //            string numberOccurency = filteredTitle.Substring(0, indx);              // Get occurrency from title
        //            numberOccurency        = numberOccurency.Trim(' ', ']', '[');
        //            Int16.TryParse(numberOccurency, out occurency);

        //            if (occurency > 1)                                                      // This will fix the note counter due to the
        //                --occurency;                                                        // displayed note number that starts from "[2]"
                
        //            filteredTitle = filteredTitle.Substring(indx + 1).Trim();
        //        }

        //        if (note.Tag         != null &&                                             // Get the node
        //            note.description == desc &&
        //            note.isDialog    == isDiag &&
        //            filteredTitle    == tit &&
        //            note.type        == type)
        //        {
        //            string hex_id          = note.Tag.ToString().Split('|')[1];             // Get Notifier ID
        //            short id               = Convert.ToInt16(hex_id, 16);
        //            updated_note_id        = id;
        //            updated_note_occurency = ++occurency;
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        
        /// <summary>
        /// Update the note with the new content. Reset the timeout if any
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="desc"></param>
        /// <param name="noteType"></param>
        /// <param name="title"></param>
        //public static void Update(short ID, string desc, Type noteType, string title)
        //{
        //    foreach (var note in notes)
        //    {
        //        if (note.Tag != null &&                                     // Get the node
        //            note.Tag.Equals("__Notifier|" + ID.ToString("X4")))
        //        {
        //            if (note.timerResetEvent != null)                            // Reset the timeout timer (if any)
        //                note.timerResetEvent.Set();

        //            Notification myNote = (Notification)note;
        //            myNote.setNotifier(desc, noteType, title, true);        // Set the new note content
        //        }
        //    }
        //}
        
        /// <summary>
        /// Background Worker to handle the timeout of the note
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void timer_DoWork(object sender, DoWorkEventArgs e)
        {
            Notification not = (Notification)e.Argument;
            bool timedOut = false;
            while (!timedOut)
            {
                if (!not.timerResetEvent.WaitOne(not.timeout_ms))
                    timedOut = true;                                        // Time is out
            }
            e.Result = e.Argument;
        }
        
        /// <summary>
        /// Background Worker to handle the timeout event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void timer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Notification not = (Notification) e.Result;
            not.closeMe();                                                  // Close the note
        }
        
        /// <summary>
        /// Show a Dialog note: with faded background if specified
        /// </summary>
        /// <param name="content"></param>
        /// <param name="type"></param>
        /// <param name="title"></param>
        /// <param name="backDialogStyle"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        public static DialogResult ShowDialog(string content, Type type = Type.INFO, string title = "Notifier",
                                              BackDialogStyle backDialogStyle = BackDialogStyle.FadedScreen,Form application = null)
        {
            Form back               = null;
            int backBorder          = 200;
            bool orgTopMostSettings = false;

            if (backDialogStyle == BackDialogStyle.FadedApplication && 
                application     == null)
                backDialogStyle     = BackDialogStyle.FadedScreen;

            if (backDialogStyle != BackDialogStyle.None)
            {
                back = new Form();                                              // Create the fade background
                back.FormBorderStyle = FormBorderStyle.None;
                back.BackColor       = Color.FromArgb(0, 0, 0);
                back.Opacity         = 0.6;
                back.ShowInTaskbar   = false;
            }

            Notification note           = new Notification(content, type, title, true);      // Instantiate the Notifier form
            note.backDialogStyle    = backDialogStyle;

            switch (note.backDialogStyle)
            {
                case BackDialogStyle.None:
                    if (application != null)                                    // Set the startup position
                    {
                        note.Owner         = application;
                        note.StartPosition = FormStartPosition.CenterParent;
                    }
                    else
                    {
                        note.StartPosition = FormStartPosition.CenterScreen;
                    }
                    break;
                case BackDialogStyle.FadedScreen:
                    back.Location          = new System.Drawing.Point(-backBorder, -backBorder);
                    back.Size              = new Size(Screen.PrimaryScreen.WorkingArea.Width + backBorder,
                                                      Screen.PrimaryScreen.WorkingArea.Height + backBorder);

                    if (application != null)
                        back.Show(application);
                    back.TopMost           = true;
                    note.StartPosition     = FormStartPosition.CenterScreen;    // Set the startup position
                    break;
                case BackDialogStyle.FadedApplication:
                    note.myCallerApp       = application;
                    orgTopMostSettings     = application.TopMost;
                    application.TopMost    = true;
                    back.StartPosition     = FormStartPosition.Manual;
                    back.Size              = application.Size;
                    back.Location          = application.Location;
                    if (application != null)
                        back.Show(application);
                    back.TopMost           = true;
                    note.StartPosition     = FormStartPosition.CenterParent;    // Set the startup position
                    break;
            }

            notes.Add(note);                                                    // Add to our collection of Notifiers    
            note.ShowInTaskbar = false;
            note.ShowDialog();

            if (back != null)                                                   // Close the back
                back.Close();

            if (application != null)                                            // restore app window top most property
                application.TopMost = orgTopMostSettings;

            return DialogResult.OK;
        }
        
        /// <summary>
        /// Show a Dialog note: fast creation
        /// </summary>
        /// <param name="content"></param>
        /// <param name="title"></param>
        /// <param name="type"></param>
        public static void ShowDialog(string content, string title = "Notifier", Type type = Type.INFO)
        {
            ShowDialog(content, type, title);
        }
        
        /// <summary>
        /// Handle the dragging event: change the position of the note
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (noteLocation.mouseIsDown)
            {
                int xDiff = noteLocation.initialLocation.X - e.Location.X;      // Get the difference between the two points
                int yDiff = noteLocation.initialLocation.Y - e.Location.Y;

                int x = this.Location.X - xDiff;                                // Set the new point
                int y = this.Location.Y - yDiff;

                noteLocation.X = x;                                             // Update the location
                noteLocation.Y = y;
                Location = new Point(x, y);
            }
        }
        
        /// <summary>
        /// Handle the mouse down event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            noteLocation.initialLocation = e.Location;
            noteLocation.mouseIsDown = true;
        }
        
        /// <summary>
        /// Handle the mouse up event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            noteLocation.mouseIsDown = false;
        }

    }  
     
}       
