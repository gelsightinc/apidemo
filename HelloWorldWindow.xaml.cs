using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GelSight.Api.Client;
using GelSight.Api.Client.EventArgs;
//using Newtonsoft.Json.Linq;
using ConnectionState = GelSight.Api.Client.ConnectionState;

namespace GelSight.Api.HelloWorld
{
    /// <summary>
    /// Interaction logic for HelloWorldWindow.xaml
    /// </summary>
    public partial class HelloWorldWindow
    {
        private readonly ConnectionManager _connectionManager;

        public HelloWorldWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Subscribe to the window closing event so we can notify the server that the client is going away
            Closing += OnClosing;

            var          host     = Utility.GetLocalIpAddress();
            const ushort port     = 9002;
            const string password = "password";

            // Update the user interface
            IpAddress.Text = host;
            Port.Text = port.ToString();
            Password.Text = password;

            // Create the ConnectionManager object
            _connectionManager = new ConnectionManager(host, port, password);

            SubscribeToServerEvents(_connectionManager);

            ConnectToServer();
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            // Close the connection to the server
            _connectionManager.Close();

            UnsubscribeFromServerEvents(_connectionManager);
        }

        private void AddLine(string message)
        {
            Dispatcher.Invoke(() =>
            {
                MessageList.Items.Add(message);
            });
        }

        private void SubscribeToServerEvents(ConnectionManager? cm)
        {
            if (cm == null)
                return;

            cm.ConnectionStateChanged += ConnectionStateChanged;
            cm.StatusChanged          += StatusChanged;
            cm.ErrorMessageReceived   += ErrorMessageReceived;
            cm.GelChanged             += GelChanged;
            cm.ScanCompleted          += ScanCompleted;
            cm.AnalysisSaved          += AnalysisSaved;
            cm.HeightmapStarted       += HeightmapStarted;
            cm.HeightmapCanceled      += HeightmapCanceled;
            cm.HeightmapCompleted     += HeightmapCompleted;
            cm.ScanDeleted            += ScanDeleted;
            cm.GelStateChanged        += GelStateChanged;
            cm.ImageAcquired          += ImageAcquired;
        }

        private void UnsubscribeFromServerEvents(ConnectionManager? cm)
        {
            if (cm == null)
                return;

            cm.ConnectionStateChanged -= ConnectionStateChanged;
            cm.StatusChanged          -= StatusChanged;
            cm.ErrorMessageReceived   -= ErrorMessageReceived;
            cm.GelChanged             -= GelChanged;
            cm.ScanCompleted          -= ScanCompleted;
            cm.AnalysisSaved          -= AnalysisSaved;
            cm.HeightmapStarted       -= HeightmapStarted;
            cm.HeightmapCanceled      -= HeightmapCanceled;
            cm.HeightmapCompleted     -= HeightmapCompleted;
            cm.ScanDeleted            -= ScanDeleted;
            cm.GelStateChanged        -= GelStateChanged;
            cm.ImageAcquired          -= ImageAcquired;
        }

        //
        // Server event handlers
        //
        
        private void ScanDeleted(object? sender, ScanDeletedEventArgs args)
        {
            AddLine($"Scan deleted: ScanFolder = {args.ScanFolder}, Success = {args.Success}");
        }

        private void HeightmapCompleted(object? sender, HeightmapCompletedEventArgs args)
        {
            AddLine($"Heightmap completed: ScanFolder = {args.ScanFolder}, Success = {args.Success}");
        }

        private void HeightmapCanceled(object? sender, EventArgs args)
        {
            AddLine("Heightmap canceled.");
        }
        
        private void HeightmapStarted(object? sender, string scanFolder)
        {
            AddLine("Heightmap started.");
        }

        private void ScanCompleted(object? sender, ScanCompletedEventArgs args)
        {
            AddLine($"Scan completed: ScanFolder = {args.ScanFolder}, ScanMetadata = {args.ScanMetadata}");
        }

        private void AnalysisSaved(object? sender, AnalysisSavedEventArgs args)
        {
            // Demonstration of extracting properties from an analysis method
            // using the Nuget package, Newtonsoft's Json.NET
            // https://www.newtonsoft.com/json

            //var json = args.Results;
            //dynamic data = JObject.Parse(json);
            //foreach (var routine in data.Routines)
            //{
            //    if (routine["name"] != "Surface Roughness")
            //        continue;
                
            //    var sa = routine["Sa"];
            //    AddLine($"Sa = {sa}");
            //}
                       
            AddLine($"Analysis saved: ScanFolder = {args.ScanFolder}, Success = {args.Success}, Results = {args.Results}, ErrorMessage = {args.ErrorMessage}, RequestId = {args.RequestId}");
        }

        private void GelChanged(object? sender, GelInfo gelInfo)
        {
            Dispatcher.Invoke(() =>
            {
                var serial = $"Gel Serial: {gelInfo.Serial ?? " Unknown"}";
                var name = $"Gel Type: {gelInfo.Type ?? " Unknown"}";
                var useBy = $"Gel Use by: {gelInfo.UseBy?.ToShortDateString() ?? " Unknown"}";
                GelInfo.Content = string.Join("\t", serial, name, useBy);
            });

            AddLine($"Gel info changed: Serial = {gelInfo.Serial}, Type = {gelInfo.Type}, UseBy = {gelInfo.UseBy}");
        }

        private void GelStateChanged(object? sender, string args)
        {
            AddLine($"Gel state changed: state = {args}");
        }

        private void ImageAcquired(object? sender, MessageImageAcquiredEventArgs args)
        {
            // Call the helper method to convert the array of bytes representing
            // a jpeg image to a BitmapImage object to display in the UI
            var image = BitmapImageFromJpegArray(args.JpegImage);
            
            Dispatcher.Invoke(() =>
            {                
                ScanImage.Source = image;
            });
        }

        /// <summary>
        /// Helper method to convert an array of bytes representing a jpeg image to a BitmapImage object.
        /// </summary>
        private static BitmapImage? BitmapImageFromJpegArray(byte[] data)
        {
            if (data.Length == 0)
                return null;

            using var ms = new MemoryStream(data);

            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = ms;
            img.EndInit();

            if (img.CanFreeze)
                img.Freeze();

            return img;
        }

        private void ErrorMessageReceived(object? sender, ErrorMessageReceivedEventArgs args)
        {
            AddLine($"Error message received: Message = {args.Message}, RequestId = {args.RequestId}");
        }

        private void StatusChanged(object? sender, Status status)
        {
            Dispatcher.Invoke(() =>
            {
                SensorPresent.IsChecked = status.IsSensorPresent;
                SensorLive.IsChecked = status.IsSensorLive;
                ScanPossible.IsChecked = status.IsScanPossible;
            });

            DeviceSerialNumber = status.Serial ?? string.Empty;
            
            AddLine($"Status changed: IsSensorPresent = {status.IsSensorPresent}, IsSensorLive = {status.IsSensorLive}, IsScanPossible = {status.IsScanPossible}, " +
                    $"Model = {status.Model}, DisplaySerial = {status.DisplaySerial}, Serial = {status.Serial}, Configuration = {status.Configuration}, " +
                    $"FirmwareVersion = {status.FirmwareVersion}, ImageWidth = {status.ImageWidth}, ImageHeight = {status.ImageHeight}");
        }

        private void UpdateConnectionState()
        {
            var connectionState = _connectionManager.ConnectionState;
            
            Dispatcher.Invoke(() =>
            {
                Status.Content           = connectionState.GetDescription();
                var color = connectionState.GetColor();
                var converted = Color.FromArgb(color.A, color.R, color.G, color.B);
                Status.Background        = new SolidColorBrush(converted);
                ConnectBtn.IsEnabled     = CanConnect;
                IpAddress.IsEnabled      = CanConnect;
                Port.IsEnabled           = CanConnect;
                Password.IsEnabled       = CanConnect;
                DisconnectBtn.IsEnabled  = CanDisconnect;
            });
        }
        
        private void ConnectionStateChanged(object? sender, ConnectionState connectionState)
        {
            UpdateConnectionState();
        }

        //
        // UI helper functions
        //

        private bool CanDisconnect => _connectionManager.ConnectionState == ConnectionState.ConnectedAndValidated ||
                                      _connectionManager.ConnectionState == ConnectionState.Connected ||
                                      _connectionManager.ConnectionState == ConnectionState.Connecting;
        private bool CanConnect    => _connectionManager.ConnectionState == ConnectionState.NotStarted ||
                                      _connectionManager.ConnectionState == ConnectionState.ValidationFailed ||
                                      _connectionManager.ConnectionState == ConnectionState.VersionMismatch;

        //
        // Button click event handlers to demonstrate server request actions.
        //

        private void RequestHeightmap_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var scanFolder = @"C:\Users\Public\Documents\GelSight\Scans\New Scans\Scan008";

                var requestId = _connectionManager.RequestHeightmap(scanFolder, true, true, true, 4);
                AddLine($"Message sent: RequestId = {requestId}");
            }
            catch (Exception ex)
            {
                AddLine(ex.Message);
            }
        }

        private void TryDeleteScan_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var scanFolder = @"C:\Users\Public\Documents\GelSight\Scans\New Scans\Scan008";

                var requestId = _connectionManager.TryDeleteScan(scanFolder);
                AddLine($"Message sent: RequestId = {requestId}");
            }
            catch (Exception ex)
            {
                AddLine(ex.Message);
            }
        }

        private void RequestLiveView_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var requestId = _connectionManager.RequestLiveView(true);
                AddLine($"Message sent: RequestId = {requestId}");
            }
            catch (Exception ex)
            {
                AddLine(ex.Message);
            }
        }

        private void StopLiveView_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var requestId = _connectionManager.RequestLiveView(false);
                AddLine($"Message sent: RequestId = {requestId}");
            }
            catch (Exception ex)
            {
                AddLine(ex.Message);
            }
        }

        private void SubscribeGelState_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SubscribeToGelState = true;
                var requestId = _connectionManager.UpdateSubscriptions(SubscribeToLiveImages, SubscribeToGelState, DeviceSerialNumber);
                AddLine($"Message sent: RequestId = {requestId}");
            }
            catch (Exception ex)
            {
                AddLine(ex.Message);
            }
        }

        private void UnsubscribeGelState_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SubscribeToGelState = false;
                var requestId = _connectionManager.UpdateSubscriptions(SubscribeToLiveImages, SubscribeToGelState, DeviceSerialNumber);
                AddLine($"Message sent: RequestId = {requestId}");
            }
            catch (Exception ex)
            {
                AddLine(ex.Message);
            }
        }

        private void SubscribeLiveView_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SubscribeToLiveImages = true;
                var        requestId  = _connectionManager.UpdateSubscriptions(SubscribeToLiveImages, SubscribeToGelState, DeviceSerialNumber);
                AddLine($"Message sent: RequestId = {requestId}");
            }
            catch (Exception ex)
            {
                AddLine(ex.Message);
            }
        }

        private void UnsubscribeLiveView_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SubscribeToLiveImages = false;
                var        requestId  = _connectionManager.UpdateSubscriptions(SubscribeToLiveImages, SubscribeToGelState, DeviceSerialNumber);
                AddLine($"Message sent: RequestId = {requestId}");
            }
            catch (Exception ex)
            {
                AddLine(ex.Message);
            }
        }
        
        private void RequestScan_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var scanPath = @"C:\Users\Public\Documents\GelSight\Scans\New Scans";
                var scanPrefixName = "Test";

                var requestId = _connectionManager.RequestScan(scanPath, scanPrefixName);
                AddLine($"Message sent: RequestId = {requestId}");
            }
            catch (Exception ex)
            {
                AddLine(ex.Message);
            }
        }

        private void RequestAnalysis_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var scanFolder = @"C:\Users\Public\Documents\GelSight\Scans\New Scans\Scan008";
                var analysisName = "Surface Roughness";

                var requestId = _connectionManager.RequestAnalysis(scanFolder, analysisName);
                AddLine($"Message sent: RequestId = {requestId}");
            }
            catch (Exception ex)
            {
                AddLine(ex.Message);
            }
        }

        //
        // Connect button click handler
        //
        
        private void Connect_OnClick(object sender, RoutedEventArgs e)
        {
            ConnectToServer();
        }

        private void ConnectToServer()
        {
            try
            {
                if (!ushort.TryParse(Port.Text, out var port))
                    return;

                var host = IpAddress.Text;
                var password = Password.Text;

                _connectionManager.Port = port;
                _connectionManager.Host = host;
                _connectionManager.Password = password;

                // Attempt to connect to the GelSight server
                _ = _connectionManager.ConnectAsync();
            }
            catch (Exception ex)
            {
                AddLine(ex.Message);
            }
        }
        
        //
        // Disconnect button click handler
        //
        
        private void Disconnect_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disconnect from the GelSight server
                _connectionManager.Close();
            }
            catch (Exception ex)
            {
                AddLine(ex.Message);
            }
        }

        //
        // Clear messages button click handler
        //
        
        private void Clear_OnClick(object sender, RoutedEventArgs e)
        {
            MessageList.Items.Clear();
        }

        private bool   SubscribeToLiveImages { get; set; }
        private bool   SubscribeToGelState   { get; set; }
        private string DeviceSerialNumber    { get; set; } = string.Empty;
    }
}