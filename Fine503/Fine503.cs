using Fine503.Emums;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;

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
        public void MoveAbsolute(Axis channel, int[] movement,out string result) {
            CheckParameter(channel, movement);
            var cmd = "";
            if (channel == Axis.All) {
                Debug.Assert(movement.Length == 3);
                var cmdParam = movement.Select(step => $"{GetSign(step)}P{step}");
                cmd = $"A:W{string.Join("", cmdParam)}";
            } else {
                Debug.Assert(movement.Length == 1);
                cmd = $"A:{(int)channel}{GetSign(movement[0])}P{movement[0]}";
            }
            Drive(cmd,out result);
        }
        public void MoveRelative(Axis channel, int[] movement) {

        }
        public void MoveContinous(Axis channel, bool[] isPositive) {

        }
        private void Drive(string cmd, out string result) {
            _port.WriteLine(cmd);
            _port.WriteLine("G:");
            result = _port.ReadLine();
        }
        public void ReturnMechanicalOrigin(Axis channel) {

        }
        public void ReturnLogicalOrigin(Axis channel) {

        }
        public void StopAxis(Axis channel) {

        }
        public void StopAndGoToMechanicalOrigin() {

        }
        public void ClearCoordinateValue(Axis channel) {

        }
        public void SetStepAmount(Axis channel, int[] steps) {

        }
        public void HysteresisCurveDataAcquisition() {

        }
        public void SetClosedLoopMode(ClosedLoopMode mode) {

        }
        public void GetStatus(out int[] steps, out char[] state) {

        }
        public void GetVoltage(Axis channel, out int[] voltage) {

        }
        public void GetACK3Status(out char status) {

        }
        public void GetModelName(out string modelName) {

        }
        public void GetVersionNumber(out string version) {

        }
        public void GetSpeedNumber(Axis channel, out int speed) {

        }
        public void GetControlMode(Axis channel, out int controlMode) {

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
        private char GetSign(int value) {
            return value >=0 ? '+' : '-';
        }
    }
}