﻿using RetroSpy.Readers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using System.Management;

namespace RetroSpy
{
    public partial class SetupWindow : Window
    {
        private SetupWindowViewModel _vm;
        private DispatcherTimer _portListUpdateTimer;
        private DispatcherTimer _xiAndGamepadListUpdateTimer;
        private List<Skin> _skins;
        private List<string> _excludedSources;

        public SetupWindow()
        {
            InitializeComponent();
            _vm = new SetupWindowViewModel();
            DataContext = _vm;
            _excludedSources = new List<string>();

            if (!Directory.Exists("skins"))
            {
                MessageBox.Show("Could not find skins folder!", "RetroSpy", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            Skin.LoadResults results = Skin.LoadAllSkinsFromParentFolder("skins");
            _skins = results.SkinsLoaded;

            _vm.Skins.UpdateContents(_skins.Where(x => x.Type == InputSource.DEFAULT));

            string[] hiddenConsoles = Properties.Settings.Default.HiddleConsoleList.Split(';');
            foreach (string source in hiddenConsoles)
            {
                if (source != "")
                {
                    _excludedSources.Add(source);
                }
            }

            PopulateSources();

            _vm.DelayInMilliseconds = Properties.Settings.Default.Delay;

            _vm.StaticViewerWindowName = Properties.Settings.Default.StaticViewerWindowName;

            _portListUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _portListUpdateTimer.Tick += (sender, e) => UpdatePortList();
            _portListUpdateTimer.Start();

            _xiAndGamepadListUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _xiAndGamepadListUpdateTimer.Tick += (sender, e) =>
            {
                if (_vm.Sources.SelectedItem == InputSource.PAD)
                {
                    UpdateGamepadList();
                }
                else if (_vm.Sources.SelectedItem == InputSource.PC360)
                {
                    UpdateXIList();
                }
                //else if (_vm.Sources.SelectedItem == InputSource.XBOX)
                //{
                //    updateBeagleList();
                //}
                //else if (_vm.Sources.SelectedItem == InputSource.WII)
                //{
                //    updateBeagleI2CList();
                //}
            };
            _xiAndGamepadListUpdateTimer.Start();

            UpdatePortList();
            _vm.Ports.SelectIdFromText(Properties.Settings.Default.Port);
            _vm.Ports2.SelectIdFromText(Properties.Settings.Default.Port2);
            _vm.XIAndGamepad.SelectFirst();
            _vm.Sources.SelectId(Properties.Settings.Default.Source);
            _vm.Skins.SelectId(Properties.Settings.Default.Skin);
            _vm.Hostname = Properties.Settings.Default.Hostname;

            if (results.ParseErrors.Count > 0)
            {
                ShowSkinParseErrors(results.ParseErrors);
            }
        }

        private void PopulateSources()
        {
            List<InputSource> prunedSources = new List<InputSource>();
            foreach (InputSource source in InputSource.ALL)
            {
                if (!_excludedSources.Contains(source.Name))
                {
                    prunedSources.Add(source);
                }
            }
            _vm.Sources.UpdateContents(prunedSources);
        }

        private void ShowSkinParseErrors(List<string> errs)
        {
            StringBuilder msg = new StringBuilder();
            msg.AppendLine("Some skins were unable to be parsed:");
            foreach (string err in errs)
            {
                msg.AppendLine(err);
            }

            MessageBox.Show(msg.ToString(), "RetroSpy", MessageBoxButton.OK, MessageBoxImage.Error);
        }


        private void UpdatePortList()
        {

            List<string> arduinoPorts = GetArduinoPorts();
            GetTeensyPorts(arduinoPorts);

            string[] ports = arduinoPorts.ToArray<string>();
            if (ports.Length == 0)
            {
                ports = new string[1];
                ports[0] = "No Arduino/Teensy Found";
                _vm.Ports.UpdateContents(ports);
                _vm.Ports2.UpdateContents(ports);
            }
            else
            {
                _vm.Ports.UpdateContents(ports);
                string[] ports2 = new string[ports.Length + 1];
                ports2[0] = "Not Connected";
                for (int i = 0; i < ports.Length; ++i)
                {
                    ports2[i + 1] = ports[i];
                }
                _vm.Ports2.UpdateContents(ports2);
            }
        }

        private static void GetTeensyPorts(List<string> arduinoPorts)
        {
            const uint vid = 0x16C0;
            const uint serPid = 0x483;
            string vidStr = "'%USB_VID[_]" + vid.ToString("X") + "%'";
            using (var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE " + vidStr))
            {
                foreach (var mgmtObject in searcher.Get())
                {
                    var DeviceIdParts = ((string)mgmtObject["PNPDeviceID"]).Split("\\".ToArray());
                    if (DeviceIdParts[0] != "USB") break;

                    int start = DeviceIdParts[1].IndexOf("PID_") + 4;
                    uint pid = Convert.ToUInt32(DeviceIdParts[1].Substring(start, 4), 16);
                    if (pid == serPid)
                    {
                        uint serNum = Convert.ToUInt32(DeviceIdParts[2]);
                        string port = (((string)mgmtObject["Caption"]).Split("()".ToArray()))[1];

                        var hwid = ((string[])mgmtObject["HardwareID"])[0];
                        switch (hwid.Substring(hwid.IndexOf("REV_") + 4, 4))
                        {
                            case "0273":
                                //board = PJRC_Board.Teensy_LC;
                                break;
                            case "0274":
                                //board = PJRC_Board.Teensy_30;
                                break;
                            case "0275":
                                //board = PJRC_Board.Teensy_31_2;
                                break;
                            case "0276":
                                arduinoPorts.Add(port + " (Teensy 3.5)");
                                break;
                            case "0277":
                                arduinoPorts.Add(port + " (Teensy 3.6)");
                                break;
                            case "0279":
                                //board = PJRC_Board.Teensy_40;
                                break;
                            default:
                                //board = PJRC_Board.unknown;
                                break;
                        }
                    }
                }
            }
        }

        private static List<string> GetArduinoPorts()
        {
            List<string> arduinoPorts = new List<string>();
            ManagementScope connectionScope = new ManagementScope();
            SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);

            try
            {
                foreach (ManagementObject item in searcher.Get())
                {
                    string desc = item["Description"].ToString();
                    string deviceId = item["DeviceID"].ToString();

                    if (desc.Contains("Arduino"))
                    {
                        arduinoPorts.Add(deviceId + " (" + desc + ")");
                    }
                }
            }
            catch (ManagementException)
            {
                /* Do Nothing */
            }

            return arduinoPorts;
        }

        private void UpdateGamepadList()
        {
            _vm.XIAndGamepad.UpdateContents(GamepadReader.GetDevices());
        }

        private void UpdateXIList()
        {
            _vm.XIAndGamepad.UpdateContents(XInputReader.GetDevices());
        }

        private void UpdateBeagleList()
        {
            //_vm.XIAndGamepad.UpdateContents(XboxReader.GetDevices());
        }

        private void UpdateBeagleI2CList()
        {
            //_vm.XIAndGamepad.UpdateContents(WiiReaderV1.GetDevices());
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            Properties.Settings.Default.Port = _vm.Ports.SelectedItem;
            Properties.Settings.Default.Port2 = _vm.Ports2.SelectedItem;
            Properties.Settings.Default.Source = _vm.Sources.GetSelectedId();
            Properties.Settings.Default.Skin = _vm.Skins.GetSelectedId();
            Properties.Settings.Default.Delay = _vm.DelayInMilliseconds;
            Properties.Settings.Default.Background = _vm.Backgrounds.GetSelectedId();
            Properties.Settings.Default.Hostname = _vm.Hostname;
            Properties.Settings.Default.StaticViewerWindowName = _vm.StaticViewerWindowName;
            Properties.Settings.Default.Save();

            try
            {
                IControllerReader reader;
                if (_vm.Sources.SelectedItem == InputSource.PAD)
                {
                    reader = _vm.Sources.SelectedItem.BuildReader(_vm.XIAndGamepad.SelectedItem.ToString());
                }
                else if (_vm.Sources.SelectedItem == InputSource.PC360)
                {
                    reader = _vm.Sources.SelectedItem.BuildReader(_vm.XIAndGamepad.SelectedItem.ToString());
                }
                else if (_vm.Sources.SelectedItem == InputSource.PCKEYBOARD)
                {
                    reader = _vm.Sources.SelectedItem.BuildReader("0"); // Dummy parameter
                }
                else if (_vm.Sources.SelectedItem == InputSource.XBOX || _vm.Sources.SelectedItem == InputSource.PSCLASSIC ||
                         _vm.Sources.SelectedItem == InputSource.SWITCH || _vm.Sources.SelectedItem == InputSource.XBOX360 ||
                         _vm.Sources.SelectedItem == InputSource.GENMINI || _vm.Sources.SelectedItem == InputSource.C64MINI ||
                         _vm.Sources.SelectedItem == InputSource.NEOGEOMINI || _vm.Sources.SelectedItem == InputSource.PS3
                         || _vm.Sources.SelectedItem == InputSource.PS4 || _vm.Sources.SelectedItem == InputSource.MISTER)
                {
                    reader = _vm.Sources.SelectedItem.BuildReader(txtHostname.Text);
                }
                else if (_vm.Sources.SelectedItem == InputSource.PADDLES || _vm.Sources.SelectedItem == InputSource.CD32 || _vm.Sources.SelectedItem == InputSource.ATARI5200)
                {
                    if (_vm.Ports.SelectedItem == _vm.Ports2.SelectedItem)
                    {
                        throw new Exception("Port 1 and Port 2 cannot be the same!");
                    }

                    reader = _vm.Sources.SelectedItem.BuildReader2(_vm.Ports.SelectedItem, _vm.Ports2.SelectedItem);
                }
                //else if (_vm.Sources.SelectedItem == InputSource.XBOX)
                //{
                //    reader = _vm.Sources.SelectedItem.BuildReader(_vm.XIAndGamepad.SelectedItem.ToString());
                //}
                //else if (_vm.Sources.SelectedItem == InputSource.WII)
                //{
                //    reader = _vm.Sources.SelectedItem.BuildReader(_vm.XIAndGamepad.SelectedItem.ToString());
                //}
                else
                {
                    reader = _vm.Sources.SelectedItem.BuildReader(_vm.Ports.SelectedItem);
                }
                if (_vm.DelayInMilliseconds > 0)
                {
                    reader = new DelayedControllerReader(reader, _vm.DelayInMilliseconds);
                }

                new ViewWindow(_vm.Skins.SelectedItem,
                                _vm.Backgrounds.SelectedItem,
                                reader, _vm.StaticViewerWindowName)
                    .ShowDialog();
            }
#if DEBUG
            catch (ConfigParseException ex) {
#else
            catch (Exception ex)
            {
#endif
                MessageBox.Show(ex.Message, "RetroSpy", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Show();
        }

        private void SourceSelectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vm.Sources.SelectedItem == null)
            {
                return;
            }

            _vm.ComPortOptionVisibility = _vm.Sources.SelectedItem.RequiresComPort ? Visibility.Visible : Visibility.Hidden;
            _vm.ComPort2OptionVisibility = _vm.Sources.SelectedItem.RequiresComPort2 ? Visibility.Visible : Visibility.Hidden;
            _vm.XIAndGamepadOptionVisibility = _vm.Sources.SelectedItem.RequiresId ? Visibility.Visible : Visibility.Hidden;
            _vm.SSHOptionVisibility = _vm.Sources.SelectedItem.RequiresHostname ? Visibility.Visible : Visibility.Hidden;
            UpdateGamepadList();
            UpdateXIList();
            UpdatePortList();
            UpdateBeagleList();
            UpdateBeagleI2CList();
            _vm.Skins.UpdateContents(_skins.Where(x => x.Type == _vm.Sources.SelectedItem));
            _vm.Skins.SelectFirst();
            if (_vm.Sources.GetSelectedId() == Properties.Settings.Default.Source)
            {
                _vm.Skins.SelectId(Properties.Settings.Default.Skin);
            }
        }

        private void Skin_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vm.Skins.SelectedItem == null)
            {
                return;
            }

            _vm.Backgrounds.UpdateContents(_vm.Skins.SelectedItem.Backgrounds);
            _vm.Backgrounds.SelectFirst();
            if (_vm.Skins.GetSelectedId() == Properties.Settings.Default.Skin)
            {
                _vm.Backgrounds.SelectId(Properties.Settings.Default.Background);
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(string.Format("RetroSpy Version {0}", Assembly.GetEntryAssembly().GetName().Version), "About",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddRemove_Click(object sender, RoutedEventArgs e)
        {
            new AddRemoveWindow(InputSource.ALL, _excludedSources).ShowDialog();

            PopulateSources();

            string hiddenConsoleList = "";
            foreach (string source in _excludedSources)
            {
                hiddenConsoleList += source + ";";
            }

            Properties.Settings.Default.HiddleConsoleList = hiddenConsoleList;
            Properties.Settings.Default.Save();
        }
    }

    public class SetupWindowViewModel : INotifyPropertyChanged
    {
        public class ListView<T>
        {
            private List<T> _items;

            public CollectionView Items { get; private set; }
            public T SelectedItem { get; set; }

            public ListView()
            {
                _items = new List<T>();
                Items = new CollectionView(_items);
            }

            public void UpdateContents(IEnumerable<T> items)
            {
                _items.Clear();
                _items.AddRange(items);
                Items.Refresh();
            }

            public void SelectFirst()
            {
                if (_items.Count > 0)
                {
                    SelectedItem = _items[0];
                }
            }

            public void SelectId(int id)
            {
                if (_items.Count > 0 && id >= 0 && id < _items.Count)
                {
                    SelectedItem = _items[id];
                }
                else
                {
                    SelectFirst();
                }
            }

            public void SelectIdFromText(T text)
            {
                int index = _items.IndexOf(text);
                SelectId(index);
            }

            public int GetSelectedId()
            {
                if (SelectedItem != null)
                {
                    return _items.IndexOf(SelectedItem);
                }
                return -1;
            }
        }

        public ListView<string> Ports { get; set; }
        public ListView<string> Ports2 { get; set; }
        public ListView<uint> XIAndGamepad { get; set; }
        public ListView<Skin> Skins { get; set; }
        public ListView<Skin.Background> Backgrounds { get; set; }
        public ListView<InputSource> Sources { get; set; }
        public int DelayInMilliseconds { get; set; }
        public bool StaticViewerWindowName { get; set; }
        public string Hostname { get; set; }

        private Visibility _comPortOptionVisibility;

        public Visibility ComPortOptionVisibility
        {
            get => _comPortOptionVisibility;
            set
            {
                _comPortOptionVisibility = value;
                NotifyPropertyChanged("ComPortOptionVisibility");
            }
        }

        private Visibility _comPort2OptionVisibility;

        public Visibility ComPort2OptionVisibility
        {
            get => _comPort2OptionVisibility;
            set
            {
                _comPort2OptionVisibility = value;
                NotifyPropertyChanged("ComPort2OptionVisibility");
            }
        }

        private Visibility _XIAndGamepadOptionVisibility;

        public Visibility XIAndGamepadOptionVisibility
        {
            get => _XIAndGamepadOptionVisibility;
            set
            {
                _XIAndGamepadOptionVisibility = value;
                NotifyPropertyChanged("XIAndGamepadOptionVisibility");
            }
        }

        private Visibility _SSHOptionVisibility;

        public Visibility SSHOptionVisibility
        {
            get => _SSHOptionVisibility;
            set
            {
                _SSHOptionVisibility = value;
                NotifyPropertyChanged("SSHOptionVisibility");
            }
        }

        public SetupWindowViewModel()
        {
            Ports = new ListView<string>();
            Ports2 = new ListView<string>();
            XIAndGamepad = new ListView<uint>();
            Skins = new ListView<Skin>();
            Sources = new ListView<InputSource>();
            Backgrounds = new ListView<Skin.Background>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string prop)
        {
            if (PropertyChanged == null)
            {
                return;
            }

            PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}