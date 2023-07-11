using Fine503.Emums;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace Fine503 {
    public class Fine503 {
        private readonly SerialPort _port;
        public bool IsOpen => _port.IsOpen;
        /// <summary>
        /// 透過RS232連接FINE-503
        /// </summary>
        /// <param name="portName">序列埠名稱</param>
        /// <param name="baudRate">序列埠鮑率，請和FINE-503的Memory Switch內的設定相同</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public Fine503(string portName, int baudRate) {
            if (string.IsNullOrEmpty(portName)) {
                throw new ArgumentNullException(nameof(portName));
            }
            if (baudRate != 4800 || baudRate != 9600 || baudRate != 19200 || baudRate != 38400) {
                throw new ArgumentException("baudRate must be 4800, 9600, 19200 or 38400!");
            }
            _port = new(portName, baudRate, Parity.None, 8, StopBits.One) {
                Encoding = Encoding.ASCII,
                Handshake = Handshake.RequestToSend,
                NewLine = "\r\n",
                ReadTimeout = 3,
                WriteTimeout = 3,
            };

            try {
                _port.Open();
            }
            catch {
                throw;
            }
        }
        public void MoveAbsolute(Axis channel, int[] movement, out string result) {
            CheckParameter(channel, movement);
            var cmd = "";
            if (channel == Axis.All) {
                var cmdParam = movement.Select(step => $"{GetSign(step)}P{step}");
                cmd = $"A:W{string.Join("", cmdParam)}";
            } else {
                cmd = $"A:{(int)channel}{GetSign(movement[0])}P{movement[0]}";
            }
            Drive(cmd, out result);
        }
        public void MoveRelative(Axis channel, int[] movement, out string result) {
            CheckParameter(channel, movement);
            var cmd = "";
            if (channel == Axis.All) {
                var cmdParam = movement.Select(step => $"{GetSign(step)}P{step}");
                cmd = $"M:W{string.Join("", cmdParam)}";
            } else {
                cmd = $"M:{(int)channel}{GetSign(movement[0])}P{movement[0]}";
            }
            Drive(cmd, out result);
        }
        public void MoveContinous(Axis channel, bool[] isPositive, out string result) {
            CheckParameter(channel, isPositive);
            var cmd = "";
            if (channel == Axis.All) {
                var cmdParam = isPositive.Select(direction => GetSign(direction));
                cmd = $"J:W{string.Join("", cmdParam)}";
            } else {
                cmd = $"J:{(int)channel}{GetSign(isPositive[0])}";
            }
            Drive(cmd, out result);
        }
        private void Drive(string cmd, out string result) {
            _port.WriteLine(cmd);
            _port.WriteLine("G:");
            result = _port.ReadLine();
        }
        /// <summary>
        /// 回到機械原點
        /// </summary>
        /// <param name="channel"></param>
        public void ReturnMechanicalOrigin(Axis channel,out string result) {
            CheckAxisCorrect(channel);
            var cmd = $"H:{(int)channel}";
            if(channel == Axis.All) {
                cmd = $"H:W";
            }
            _port.WriteLine(cmd);
            result = _port.ReadLine();
        }
        public void ReturnLogicalOrigin(Axis channel,out string result) {
            CheckAxisCorrect(channel);
            var cmd = $"N:{(int)channel}";
            if (channel == Axis.All) {
                cmd = $"N:W";
            }
            _port.WriteLine(cmd);
            result = _port.ReadLine();
        }
        public void StopAxis(Axis channel,out string result) {
            CheckAxisCorrect(channel);
            var cmd = $"L:{(int)channel}";
            if (channel == Axis.All) {
                cmd = $"L:W";
            }
            _port.WriteLine(cmd);
            result = _port.ReadLine();
        }
        public void StopAndGoToMechanicalOrigin(out string result) {
            var cmd = $"L:E";
            _port.WriteLine(cmd);
            result = _port.ReadLine();
        }
        public void ClearCoordinateValue(Axis channel, out string result) {
            CheckAxisCorrect(channel);
            var cmd = $"R:{(int)channel}";
            if (channel == Axis.All) {
                cmd = $"R:W";
            }
            _port.WriteLine(cmd);
            result = _port.ReadLine();
        }
        public void SetStepAmount(Axis channel, int[] steps, out string result) {
            CheckParameter(channel, steps);
            var cmd = "";
            if (channel == Axis.All) {
                var cmdParam = steps.Select(step => $"{step}S");
                cmd = $"D:W{string.Join("", cmdParam)}";
            } else {
                cmd = $"D:{(int)channel}{steps[0]}S";
            }
            _port.WriteLine(cmd);
            result = _port.ReadLine();
        }
        public void HysteresisCurveDataAcquisition(out string result) {
            var cmd = $"@:";
            _port.WriteLine(cmd);
            result = _port.ReadLine();
        }
        public void SetClosedLoopMode(ClosedLoopMode mode, out string result) {
            var cmd = $"K:{(int)mode}";
            _port.WriteLine(cmd);
            result = _port.ReadLine();
        }
        public void GetStatus(out int[] steps, out char[] state) {
            var cmd = $"Q:";
            _port.WriteLine(cmd);
            var response = _port.ReadLine();
            var data = response.Replace(" ", "").Split(",");
            Debug.Assert(data.Length == 6);
            steps = new[] { int.Parse(data[0]), int.Parse(data[1]), int.Parse(data[2]) };
            state = new[] { data[3][0], data[4][0], data[5][0] };
        }
        public void GetVoltage(Axis channel, out int[] voltage) {
            CheckAxisCorrect(channel);
            var cmd = "";
            if (channel==Axis.All) {
                cmd = @"V:W";
            } else {
                cmd = $"V:{(int)channel}";
            }
            _port.WriteLine(cmd);
            var response = _port.ReadLine().Replace(" ", "").Split(",");
            if (response.Length == 3) {
                voltage = new[] { int.Parse(response[0]), int.Parse(response[1]), int.Parse(response[2]) };
            } else {
                voltage = new[] { int.Parse(response[0]) };
            }
        }
        public void GetACK3Status(out char status) {
            var cmd = $"!:";
            _port.WriteLine(cmd);
            var result = _port.ReadLine();
            status = result[0];
        }
        public void GetModelName(out string modelName) {
            var cmd = $"?:N";
            _port.WriteLine(cmd);
            modelName = _port.ReadLine();
        }
        public void GetVersionNumber(out string version) {
            var cmd = $"?:V";
            _port.WriteLine(cmd);
            version = _port.ReadLine();
        }
        public void GetSpeedNumber(Axis channel, out int[] speed) {
            CheckAxisCorrect(channel);
            var cmd = "";
            if (channel == Axis.All) {
                cmd = $"?:DW";
            } else {
                cmd = $"?:D{(int)channel}";
            }
            _port.WriteLine(cmd);
            var response = _port.ReadLine().Split("S", StringSplitOptions.RemoveEmptyEntries);
            if (response.Length == 3) {
                speed = new[] { int.Parse(response[0]), int.Parse(response[1]), int.Parse(response[2]) };
            } else {
                speed = new[] { int.Parse(response[0]) };
            }
        }
        public void GetControlMode(Axis channel, out int[] controlMode) {
            CheckAxisCorrect(channel);
            var cmd = "";
            if (channel == Axis.All) {
                cmd = $"?:CW";
            } else {
                cmd = $"?:C{(int)channel}";
            }
            _port.WriteLine(cmd);
            var response = _port.ReadLine().Split(",", StringSplitOptions.RemoveEmptyEntries);
            if (response.Length == 3) {
                controlMode = new[] { int.Parse(response[0]), int.Parse(response[1]), int.Parse(response[2]) };
            } else {
                controlMode = new[] { int.Parse(response[0]) };
            }
        }
        /// <summary>
        /// 檢查輸入參數數量和軸的選擇是否正確配對
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">選擇的軸</param>
        /// <param name="param">如果選單一軸，則param數量預期為1，選全部則預期為3</param>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private void CheckParameter<T>(Axis channel, T[] param) {
            var paramLengthCorrect = channel switch {
                Axis.All => param.Length == 3,
                Axis.First => param.Length == 1,
                Axis.Second => param.Length == 1,
                Axis.Third => param.Length == 1,
                _ => throw new NotImplementedException("Axis Error!")
            };
            if (!paramLengthCorrect) {
                throw new ArgumentException("Length of movement must be correct! 3 or 1");
            }
        }
        private void CheckAxisCorrect(Axis channel) {
            if (!Enum.IsDefined(channel)) {
                throw new ArgumentException("Axis Error!");
            }
        }
        /// <summary>
        /// 抓取符號，如果是數字則根據輸入參數判定符號
        /// </summary>
        /// <typeparam name="T">可為bool或是數字</typeparam>
        /// <param name="value">輸入數值</param>
        /// <returns>正負號</returns>
        /// <exception cref="ArgumentException"></exception>
        private char GetSign<T>(T value) {
            if (value is int val) {
                return val >= 0 ? '+' : '-';
            } else if (value is bool isPositive) {
                return isPositive ? '+' : '-';
            } else {
                throw new ArgumentException(nameof(value));
            }
        }
    }
}