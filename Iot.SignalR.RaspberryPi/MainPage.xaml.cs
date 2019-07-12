﻿using Windows.Devices.Gpio;
using Windows.UI.Xaml.Controls;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Drawing;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Iot.SignalR.RaspberryPi
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private GpioPin gpioPin { get; set; }
        public Brush stateColor { get; set; } = new SolidColorBrush(Windows.UI.Colors.Gray);


        public MainPage()
        {
            InitializeComponent();
            InitializeGPIO();
            ConnectSignalR();
        }

        public void InitializeGPIO()
        {
            var controller = GpioController.GetDefault();
            gpioPin = controller.OpenPin(4);

            gpioPin.Write(GpioPinValue.High);
            gpioPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        public async void ConnectSignalR()
        {
            var hubConnection = new HubConnectionBuilder()
                .WithUrl("http://tdc-dotnet-iot.azurewebsites.net/IotServerHub")
                .Build();

            hubConnection.On<bool>("SendStateToDevice", (state) =>
            {
                Debug.WriteLine($"State: {state}");
                if (state)
                    gpioPin.Write(GpioPinValue.Low);
                else
                    gpioPin.Write(GpioPinValue.High);

                hubConnection.SendAsync("ReceiveStateFromDevice", state);
            });

            hubConnection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await hubConnection.StartAsync();
            };

            await hubConnection.StartAsync();
            var pinState = gpioPin.Read() == GpioPinValue.Low;
            await hubConnection.SendAsync("ReceiveStateFromDevice", pinState);
        }

    }
}
