using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BootLoader.Impl;
using BootLoader.Interfaces;
using BootLoader.Protocol.Interface;

namespace BootLoader.Device
{
    public class TimerDeviceImpl
    {
        private IProtocol _protocol = null;
        public ITimer TimerForTimeouts { get; set; }
        
        public TimerDeviceImpl(IProtocol protocol) {
            _protocol = protocol;
            TimerForTimeouts = new Timer();
        }
    }
}
