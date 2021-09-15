using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Plugins
{
    public class UCCNCplugin //Class name must be UCCNCplugin to work! 
    {
        private enum PortStatus
        {
            CLOSED,
            OK,
            OPEN_ERROR,
            NO_RESPONSE
        };

        private static Regex mPortNameRegex = new Regex(@"^COM\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public Plugininterface.Entry UC;
        private bool mPortChanged;
        private string mPortName;
        private PortStatus mPortStatus;
        private SerialPort mPort = new SerialPort();
        private ConfigForm mConfigForm;
         
        public UCCNCplugin()
        {
            // empty
        }


        protected virtual void Dispose(bool disposing)
        {
            if ( mPort.IsOpen )
            {
                try
                {
                    mPort.Close();
                }
                catch(Exception)
                {
                    // empty
                }
            }
        }


        private bool validatePortName(string portName)
        {
            return mPortNameRegex.IsMatch(portName);
        }

        //Called when the plugin is initialised.
        //The parameter is the Plugin interface object which contains all functions prototypes for calls and callbacks.
        public void Init_event(Plugininterface.Entry UC)
        {
            this.UC = UC;

            mPortName = UC.Readkey("mpg-nano", "portName", "");
            
            if ( validatePortName(mPortName) )
            {
                mPortChanged = true;
            }
            else
            {
                mPortChanged = false;
                mPortName = "";
            }

            mPortStatus = PortStatus.CLOSED;

            mPort.BaudRate = 38400;
            mPort.DataBits = 8;
            mPort.Parity = Parity.None;
            mPort.StopBits = StopBits.One;
            mPort.Handshake = Handshake.None;
            mPort.ReadBufferSize = 32;
            mPort.WriteBufferSize = 32;
            mPort.ReadTimeout = 250;
            mPort.WriteTimeout = 250;
        }

        //Called when the plugin is loaded, the author of the plugin should set the details of the plugin here.
        public Plugininterface.Entry.Pluginproperties Getproperties_event(Plugininterface.Entry.Pluginproperties Properties)
        {
            Properties.author = "Matt Bucknall";
            Properties.pluginname = "MPG-Nano"; 
            Properties.pluginversion = "1.0.1";
            return Properties;
        }

        //Called from UCCNC when the user presses the Configuration button in the Plugin configuration menu.
        //Typically the plugin configuration window is shown to the user.
        public void Configure_event()
        {
            if (mConfigForm == null || mConfigForm.IsDisposed)
            {
                mConfigForm = new ConfigForm();
                mConfigForm.FormClosing += m_config_form_FormClosing;

                foreach (var portName in SerialPort.GetPortNames())
                {
                    mConfigForm.portName.Items.Add(portName);
                }

                mConfigForm.portName.SelectedItem = mPortName;
                mConfigForm.portName.SelectedIndexChanged += PortName_SelectedIndexChanged;

                updateStatus(mPortStatus);
            }

            mConfigForm.ShowDialog(); 
        }

        private void PortName_SelectedIndexChanged(object sender, EventArgs e)
        {
            var newPortName = mConfigForm.portName.SelectedItem.ToString().Trim();

            if (validatePortName(newPortName) && newPortName != mPortName)
            {
                mPortName = newPortName;
                mPortChanged = true;

                UC.Writekey("mpg-nano", "portName", mPortName);
            }
        }

        private void m_config_form_FormClosing(object sender, FormClosingEventArgs e)
        {
            mConfigForm = null;
        }

        private void updateStatus(PortStatus status)
        {
            mPortStatus = status;

            if ( mConfigForm != null && !mConfigForm.IsDisposed )
            {
                string statusText;

                switch(status)
                {
                    case PortStatus.CLOSED:
                        statusText = "CLOSED";
                        break;

                    case PortStatus.OK:
                        statusText = "OK";
                        break;

                    case PortStatus.NO_RESPONSE:
                        statusText = "NO RESPONSE";
                        break;

                    case PortStatus.OPEN_ERROR:
                        statusText = "OPEN ERROR";
                        break;

                    default:
                        statusText = "UNDEFINED";
                        break;
                }

                mConfigForm.portStatus.Text = statusText;
            }
        }

        //Called from UCCNC when the plugin is loaded and started.
        public void Startup_event()
        {
            
        }

        //Called when the Pluginshowup(string Pluginfilename); function is executed in the UCCNC.
        public void Showup_event()
        {
           
        }

        //Called when the UCCNC software is closing.
        public void Shutdown_event()
        {
            try
            {
                
            }
            catch (Exception) { }
        }

        //Called in a loop with a 25Hz interval.
        public void Loop_event() 
        {
            if ( mPortChanged )
            {
                mPortChanged = false;
                
                if ( mPort.IsOpen )
                {
                    mPort.Close();
                    updateStatus(PortStatus.CLOSED);
                }

                mPort.PortName = mPortName;
            }

            if ( !mPort.IsOpen && mPortName.Length > 0 )
            {
                mPort.PortName = mPortName;

                try
                {
                    mPort.Open();
                    updateStatus(PortStatus.OK);
                }
                catch (Exception)
                {
                    updateStatus(PortStatus.OPEN_ERROR);
                }
            }
        }

        //This is a direct function call addressed to this plugin dll
        //The function can be called by macros or by another plugin
        //The passed parameter is an object and the return value is also an object
        public object Informplugin_event(object Message)
        {
            return null;
        }

        //This is a function call made to all plugin dll files
        //The function can be called by macros or by another plugin
        //The passed parameter is an object and there is no return value
        public void Informplugins_event(object Message)
        {
            // empty
        }

        //Called when the user presses a button on the UCCNC GUI or if a Callbutton function is executed.
        //The int buttonnumber parameter is the ID of the caller button.
        // The bool onscreen parameter is true if the button was pressed on the GUI and is false if the Callbutton function was called.
        public void Buttonpress_event(int buttonnumber, bool onscreen)
        {
            // empty
        }

        //Called when the user clicks the toolpath viewer
        //The parameters X and Y are the click coordinates in the model space of the toolpath viewer
        //The Istopview parameter is true if the toolpath viewer is rotated into the top view,
        //because the passed coordinates are only precise when the top view is selected.
        public void Toolpathclick_event(double X, double Y, bool Istopview)
        {
            // empty
        }

        //Called when the user clicks and enters a Textfield on the screen
        //The labelnumber parameter is the ID of the accessed Textfield
        //The bool Ismainscreen parameter is true is the Textfield is on the main screen and false if it is on the jog screen
        public void Textfieldclick_event(int labelnumber, bool Ismainscreen)
        {
            // empty
        }

        //Called when the user enters text into the Textfield and it gets validated
        //The labelnumber parameter is the ID of the accessed Textfield
        //The bool Ismainscreen parameter is true is the Textfield is on the main screen and false if it is on the jog screen.
        //The text parameter is the text entered and validated by the user
        public void Textfieldtexttyped_event(int labelnumber, bool Ismainscreen, string text)
        {
            if (Ismainscreen)
            {
                if (labelnumber == 1000)
                {
                    
                }
            }
        }

        //Called when the user presses the Cycle start button and before the Cycle starts
        //This event may be used to show messages or do actions on Cycle start 
        //For example to cancel the Cycle if a condition met before the Cycle starts with calling the Button code 130 Cycle stop
        public void Cyclethreadstart_event()
        {
            
        }
    }
}
