using Fine503.Enums;
using System.IO.Ports;

var port = SerialPort.GetPortNames()[0];

using var instance = new Fine503.Fine503(port, 38400) { ShowCommandInConsole = true };

int[] steps = new int[1];
var delay = 1000;

instance.StopAndGoToMechanicalOrigin(out _);
for (int i = 15; i <= 150; i += 15) {
    steps[0] = i;
    instance.MoveAbsolute(Axis.First, steps, out _);
    instance.Drive(out _);
    await Task.Delay(delay);
}

instance.StopAndGoToMechanicalOrigin(out _);
for (int i = 25; i <= 250; i += 25) {
    steps[0] = i;
    instance.MoveAbsolute(Axis.Second, steps, out _);
    instance.Drive(out _);
    await Task.Delay(delay);
}

instance.StopAndGoToMechanicalOrigin(out _);
for (int i = 15; i <= 150; i+=15) {
    steps[0] = i;
    instance.MoveAbsolute(Axis.Third, steps, out _);
    instance.Drive(out _);
    await Task.Delay(delay);
}
