using Fine503.Enums;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace Fine503 {
    public class Fine503 : IDisposable {
        private readonly SerialPort _port;
        public bool IsOpen => _port.IsOpen;
        public bool ShowCommandInConsole { get; set; } = false;
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
            if (baudRate != 4800 && baudRate != 9600 && baudRate != 19200 && baudRate != 38400) {
                throw new ArgumentException("baudRate must be 4800, 9600, 19200 or 38400!");
            }
            _port = new(portName, baudRate, Parity.None, 8, StopBits.One) {
                Encoding = Encoding.ASCII,
                Handshake = Handshake.RequestToSend,
                NewLine = "\r\n",
                ReadTimeout = 3000,
                WriteTimeout = 3000,
            };

            try {
                _port.Open();
            }
            catch {
                throw;
            }
        }
        private void SendCommand(string cmd, out string result) {
            _port.WriteLine(cmd);
            result = _port.ReadLine();
            if (ShowCommandInConsole) {
                Console.WriteLine($"{cmd}");
                Console.WriteLine($"\t{result}");
            }
        }
        /// <summary>
        /// 設定特定軸的絕對位置
        /// </summary>
        /// <param name="channel">要設定的軸</param>
        /// <param name="movement">位移量，單一軸接受長度為1的陣列，全部軸接收長度為3的陣列</param>
        /// <param name="result">移動台回應的結果，為OK或是NG</param>
        public void MoveAbsolute(Axis channel, int[] movement, out string result) {
            CheckParameter(channel, movement);
            var cmd = "";
            if (channel == Axis.All) {
                var cmdParam = movement.Select(step => $"{GetSign(step)}P{step}");
                cmd = $"A:W{string.Join("", cmdParam)}";
            } else {
                cmd = $"A:{(int)channel}{GetSign(movement[0])}P{movement[0]}";
            }
            SendCommand(cmd, out result);
        }
        /// <summary>
        /// 設定特定軸的相對位置
        /// </summary>
        /// <param name="channel">要設定的軸</param>
        /// <param name="movement">位移量，單一軸接受長度為1的陣列，全部軸接收長度為3的陣列</param>
        /// <param name="result">移動台回應的結果，為OK或是NG</param>
        public void MoveRelative(Axis channel, int[] movement, out string result) {
            CheckParameter(channel, movement);
            var cmd = "";
            if (channel == Axis.All) {
                var cmdParam = movement.Select(step => $"{GetSign(step)}P{step}");
                cmd = $"M:W{string.Join("", cmdParam)}";
            } else {
                cmd = $"M:{(int)channel}{GetSign(movement[0])}P{movement[0]}";
            }
            SendCommand(cmd, out result);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel">要設定的軸</param>
        /// <param name="isPositive">設定是否往正方向移動，單一軸接受長度為1的陣列，全部軸接收長度為3的陣列</param>
        /// <param name="result">移動台回應的結果，為OK或是NG</param>
        public void MoveContinous(Axis channel, bool[] isPositive, out string result) {
            CheckParameter(channel, isPositive);
            var cmd = "";
            if (channel == Axis.All) {
                var cmdParam = isPositive.Select(direction => GetSign(direction));
                cmd = $"J:W{string.Join("", cmdParam)}";
            } else {
                cmd = $"J:{(int)channel}{GetSign(isPositive[0])}";
            }
            SendCommand(cmd, out result);
        }
        /// <summary>
        /// 在設定絕對位置或是相對位置或是連續移動後，需要透過這個指令來驅動移動台動作
        /// </summary>
        /// <param name="result">移動台回應的結果，為OK或是NG</param>
        public void Drive(out string result) {
            var cmd = "G:";
            SendCommand(cmd, out result);
        }
        /// <summary>
        /// 回到機械原點
        /// </summary>
        /// <param name="channel">要設定的軸</param>
        /// <param name="result">移動台回應的結果，為OK或是NG</param>
        public void ReturnMechanicalOrigin(Axis channel, out string result) {
            CheckAxisCorrect(channel);
            var cmd = $"H:{(int)channel}";
            if (channel == Axis.All) {
                cmd = $"H:W";
            }
            SendCommand(cmd, out result);
        }
        /// <summary>
        /// 回到邏輯原點
        /// </summary>
        /// <param name="channel">要設定的軸</param>
        /// <param name="result">移動台回應的結果，為OK或是NG</param>
        public void ReturnLogicalOrigin(Axis channel, out string result) {
            CheckAxisCorrect(channel);
            var cmd = $"N:{(int)channel}";
            if (channel == Axis.All) {
                cmd = $"N:W";
            }
            SendCommand(cmd, out result);
        }
        /// <summary>
        /// 停止移動台動作
        /// </summary>
        /// <param name="channel">要設定的軸</param>
        /// <param name="result">移動台回應的結果，為OK或是NG</param>
        public void StopAxis(Axis channel, out string result) {
            CheckAxisCorrect(channel);
            var cmd = $"L:{(int)channel}";
            if (channel == Axis.All) {
                cmd = $"L:W";
            }
            SendCommand(cmd, out result);
        }
        /// <summary>
        /// 立刻停止移動台動作並回到機械原點
        /// </summary>
        /// <param name="result">移動台回應的結果，為OK或是NG</param>
        public void StopAndGoToMechanicalOrigin(out string result) {
            var cmd = $"L:E";
            SendCommand(cmd, out result);
        }
        /// <summary>
        /// 清除座標值
        /// </summary>
        /// <param name="channel">要設定的軸</param>
        /// <param name="result">移動台回應的結果，為OK或是NG</param>
        public void ClearCoordinateValue(Axis channel, out string result) {
            CheckAxisCorrect(channel);
            var cmd = $"R:{(int)channel}";
            if (channel == Axis.All) {
                cmd = $"R:W";
            }
            SendCommand(cmd, out result);
        }
        /// <summary>
        /// 設定位移量
        /// </summary>
        /// <param name="channel">要設定的軸</param>
        /// <param name="steps">位移量，單一軸接受長度為1的陣列，全部軸接收長度為3的陣列，位移量範圍為1~3000步</param>
        /// <param name="result">移動台回應的結果，為OK或是NG</param>
        public void SetStepAmount(Axis channel, int[] steps, out string result) {
            CheckParameter(channel, steps);
            var cmd = "";
            if (channel == Axis.All) {
                var cmdParam = steps.Select(step => $"{step}S");
                cmd = $"D:W{string.Join("", cmdParam)}";
            } else {
                cmd = $"D:{(int)channel}{steps[0]}S";
            }
            SendCommand(cmd, out result);
        }
        /// <summary>
        /// 此命令通過感測器讀取位移值並獲取遲滯特性。開迴路時顯示0mV
        /// </summary>
        /// <param name="result">移動台回應的結果，為OK或是NG</param>
        public void HysteresisCurveDataAcquisition(out string result) {
            var cmd = $"@:";
            SendCommand(cmd, out result);
        }
        /// <summary>
        /// 設定閉迴路模式
        /// </summary>
        /// <param name="mode">閉迴路模式，可為Track或是Lock</param>
        /// <param name="result">移動台回應的結果，為OK或是NG</param>
        public void SetClosedLoopMode(ClosedLoopMode mode, out string result) {
            var cmd = $"K:{(int)mode}";
            SendCommand(cmd, out result);
        }
        /// <summary>
        /// 獲取移動台目前狀態
        /// </summary>
        /// <param name="steps">三軸目前的數值</param>
        /// <param name="state">三種狀態，請看說明書</param>
        public void GetStatus(out int[] steps, out char[] state) {
            var cmd = $"Q:";
            SendCommand(cmd, out var response);
            var data = response.Replace(" ", "").Split(",");
            Debug.Assert(data.Length == 6);
            steps = new[] { int.Parse(data[0]), int.Parse(data[1]), int.Parse(data[2]) };
            state = new[] { data[3][0], data[4][0], data[5][0] };
        }
        /// <summary>
        /// 查看供給電壓
        /// </summary>
        /// <param name="channel">要查看的軸</param>
        /// <param name="voltage">回傳的電壓值，單一軸回傳長度為1的陣列，全部軸回傳長度為3的陣列</param>
        public void GetVoltage(Axis channel, out int[] voltage) {
            CheckAxisCorrect(channel);
            var cmd = "";
            if (channel == Axis.All) {
                cmd = @"V:W";
            } else {
                cmd = $"V:{(int)channel}";
            }
            SendCommand(cmd, out var result);
            var response = result.Replace(" ", "").Split(",");
            if (response.Length == 3) {
                voltage = new[] { int.Parse(response[0]), int.Parse(response[1]), int.Parse(response[2]) };
            } else {
                voltage = new[] { int.Parse(response[0]) };
            }
        }
        /// <summary>
        /// 獲得目前移動台的命令接收能力狀態
        /// </summary>
        /// <param name="status">回傳的狀態</param>
        public void GetACK3Status(out char status) {
            var cmd = $"!:";
            SendCommand(cmd, out var result);
            status = result[0];
        }
        /// <summary>
        /// 獲得移動台機型
        /// </summary>
        /// <param name="modelName">移動台的型號</param>
        public void GetModelName(out string modelName) {
            var cmd = $"?:N";
            SendCommand(cmd, out modelName);
        }
        /// <summary>
        /// 獲得移動台版本號
        /// </summary>
        /// <param name="version">版本</param>
        public void GetVersionNumber(out string version) {
            var cmd = $"?:V";
            SendCommand(cmd, out version);
        }
        public void GetSpeedNumber(Axis channel, out int[] speed) {
            CheckAxisCorrect(channel);
            var cmd = "";
            if (channel == Axis.All) {
                cmd = $"?:DW";
            } else {
                cmd = $"?:D{(int)channel}";
            }
            SendCommand(cmd, out var result);
            var response = result.Split("S", StringSplitOptions.RemoveEmptyEntries);
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
            SendCommand(cmd, out var result);
            var response = result.Split(",", StringSplitOptions.RemoveEmptyEntries);
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

        public void Dispose() {
            _port?.Close();
            _port?.Dispose();
        }
    }
}