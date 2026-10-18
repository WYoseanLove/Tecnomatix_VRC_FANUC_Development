using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RJi.FanucRobot.Interface;
using RJi.FanucRobot.Interface.Common.Configuration;
using RJi.FanucRobot.Interface.Common.Data;
using Tecnomatix.Engineering.Ui;
using Tecnomatix.Engineering;

namespace VRC_FANUC
{
    public struct RobotSignal
    {
        public int Address;
        public string Name;
        public string Comment;
        public bool Value;
    }

    public partial class TxVRCRoboGuideComForm : TxForm
    {
        private sealed class RobotSnapshot
        {
            public List<double> AxisValues;
            public bool[] DiValues;
            public bool[] DoValues;
        }

        private struct PlcSignalWrite
        {
            public int Index;
            public int Address;
            public bool Value;
        }

        public TxRobot robot;
        public string VRC_ip;
        public int VOR_port;
        public List<double> RobotAxisValues = new List<double>();
        public List<RobotSignal> DiSignals = new List<RobotSignal>();
        public List<RobotSignal> DoSignals = new List<RobotSignal>();
        private TxPlcSignalBase[] _diSignalTargets;
        private TxPlcSignalBase[] _doSignalTargets;
        private bool[] _lastPlcToRobotValues;
        private CancellationTokenSource _monitorCancellation;
        private Task _monitorTask;
        private TxRobot _initializedRobot;
        private bool _initializingRobot;
        private bool _changingMonitorState;

        public TxVRCRoboGuideComForm()
        {
            InitializeComponent();
        }

        private void m_robotPicked_Picked(object sender, TxObjEditBoxCtrl_PickedEventArgs args)
        {
            if (_initializingRobot || _monitorCancellation != null)
            {
                return;
            }

            TxRobot pickedRobot = m_robotPicked.Object as TxRobot;
            if (pickedRobot != null && pickedRobot == _initializedRobot && _diSignalTargets != null && _doSignalTargets != null)
            {
                return;
            }

            _initializingRobot = true;
            robot = pickedRobot;
            _initializedRobot = null;
            RobotAxisValues.Clear();
            DiSignals.Clear();
            DoSignals.Clear();
            _diSignalTargets = null;
            _doSignalTargets = null;
            _lastPlcToRobotValues = null;
            m_Information.Clear();
            m_connect.Enabled = false;
            try
            {
                if (robot == null)
                {
                    VRC_ip = null;
                    VOR_port = 0;
                    m_Socketadd.Clear();
                    return;
                }

                m_robotPicked.Text = robot.Name;
                UpdateSocketAddress();
                m_robotPicked.Enabled = false;
                m_Socketadd.Enabled = false;
                if (ReadInitialRobotData())
                {
                    _initializedRobot = robot;
                    m_connect.Enabled = true;
                }
            }
            finally
            {
                m_robotPicked.Enabled = true;
                m_Socketadd.Enabled = true;
                _initializingRobot = false;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_monitorCancellation != null && _monitorTask != null && !_monitorTask.IsCompleted)
            {
                _monitorCancellation.Cancel();
            }

            base.OnFormClosed(e);
        }

        private void UpdateSocketAddress()
        {
            const int snpxPort = 60008;
            var listeningEndPoints = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Where(endPoint => endPoint.Port == snpxPort)
                .ToArray();

            VRC_ip = listeningEndPoints.Length == 0 ? null : listeningEndPoints[0].Address.ToString();
            VOR_port = listeningEndPoints.Length == 0 ? 0 : listeningEndPoints[0].Port;
            m_Socketadd.Text = string.Join(", ", listeningEndPoints.Select(endPoint => string.Format("{0}:{1}", endPoint.Address, endPoint.Port)));
        }

        private RobotConnectionConfig CreateRobotConnectionConfig()
        {
            return new RobotConnectionConfig
            {
                IpAddress = VRC_ip,
                Port = VOR_port,
                ConnectionTimeoutMs = 10000,
                ReadWriteTimeoutMs = 10000
            };
        }

        private bool ReadInitialRobotData()
        {
            const int signalPointCount = 4096;
            if (string.IsNullOrWhiteSpace(VRC_ip) || VOR_port == 0)
            {
                m_Information.Text = "未发现 RoboGuide SNPX 监听地址。";
                return false;
            }

            try
            {
                using (var snpxRobot = new FanucRobotClient(CreateRobotConnectionConfig()))
                {
                    if (!snpxRobot.Connect())
                    {
                        m_Information.Text = string.Format("无法连接 RoboGuide SNPX 服务 {0}:{1}。", VRC_ip, VOR_port);
                        return false;
                    }

                    try
                    {
                        JointPosition jointPosition = snpxRobot.Position.ReadJointPosition(group: 1);
                        bool[] diValues = snpxRobot.DI.Read(1, signalPointCount);
                        bool[] doValues = snpxRobot.DO.Read(1, signalPointCount);

                        RobotAxisValues = CreateAxisValueList(jointPosition);
                        m_Information.AppendText("读取机器人当前轴值成功。" + Environment.NewLine);
                        DiSignals = CreateSignalList(snpxRobot, "DI", diValues);
                        DoSignals = CreateSignalList(snpxRobot, "DO", doValues);
                        m_Information.AppendText("读取机器人信号表成功。" + Environment.NewLine);

                        UpdateRobotPose();
                        m_Information.AppendText("更新 Tecnomatix 机器人姿态成功。" + Environment.NewLine);
                        CreateRobotSignals();
                        TxApplication.RefreshDisplay();
                        m_Information.AppendText("创建或更新 Tecnomatix 机器人信号成功。" + Environment.NewLine);
                    }
                    finally
                    {
                        snpxRobot.Disconnect();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                AppendStatus("初始化机器人失败: " + ex.Message);
                return false;
            }
        }

        private void AppendStatus(string message)
        {
            const int maxStatusLength = 4000;
            string status = m_Information.Text + message + Environment.NewLine;
            if (status.Length > maxStatusLength)
            {
                status = status.Substring(status.Length - maxStatusLength);
            }

            m_Information.Text = status;
            m_Information.SelectionStart = m_Information.TextLength;
            m_Information.ScrollToCaret();
        }

        private void StartMonitoring()
        {
            _monitorCancellation = new CancellationTokenSource();
            _monitorTask = MonitorRobotAsync(_monitorCancellation);
        }

        private async void m_connect_Click(object sender, EventArgs e)
        {
            if (_changingMonitorState || _initializingRobot)
            {
                return;
            }

            _changingMonitorState = true;
            m_connect.Enabled = false;
            try
            {
                if (_monitorCancellation == null)
                {
                    if (_initializedRobot == null || _diSignalTargets == null || _doSignalTargets == null)
                    {
                        return;
                    }

                    m_robotPicked.Enabled = false;
                    m_Socketadd.Enabled = false;
                    StartMonitoring();
                    if (_monitorCancellation != null)
                    {
                        m_connect.Text = "Disconnect";
                        AppendStatus("已开始监听：轴值和 DI/DO 约每 100 ms 更新一次。");
                    }
                }
                else
                {
                    _monitorCancellation.Cancel();
                    await _monitorTask;
                    if (!IsDisposed)
                    {
                        AppendStatus("已断开机器人监听。");
                    }
                }
            }
            finally
            {
                _changingMonitorState = false;
                if (!IsDisposed && !Disposing)
                {
                    bool monitoring = _monitorCancellation != null;
                    m_connect.Text = monitoring ? "Disconnect" : "Connect";
                    m_connect.Enabled = _initializedRobot != null && _diSignalTargets != null && _doSignalTargets != null;
                    m_robotPicked.Enabled = !monitoring;
                    m_Socketadd.Enabled = !monitoring;
                }
            }
        }

        private async Task MonitorRobotAsync(CancellationTokenSource cancellation)
        {
            var token = cancellation.Token;
            int diCount = DiSignals.Count;
            int doCount = DoSignals.Count;
            try
            {
                using (var snpxRobot = new FanucRobotClient(CreateRobotConnectionConfig()))
                {
                    bool connected = false;
                    try
                    {
                        connected = await Task.Run(() => snpxRobot.Connect(), token);
                        if (!connected)
                        {
                            throw new InvalidOperationException("无法连接 RoboGuide SNPX 监听服务。");
                        }

                        var stopwatch = Stopwatch.StartNew();
                        long nextReadAtMs = 0;
                        long nextAxisReadAtMs = 0;
                        bool firstSnapshot = true;
                        while (!token.IsCancellationRequested)
                        {
                            List<PlcSignalWrite> writes = GetChangedPlcToRobotSignals();
                            if (writes.Count > 0)
                            {
                                await Task.Run(() => WritePlcToRobotSignals(snpxRobot, writes, token), token);
                                token.ThrowIfCancellationRequested();
                                foreach (PlcSignalWrite write in writes)
                                {
                                    _lastPlcToRobotValues[write.Index] = write.Value;
                                }
                            }

                            bool readAxes = stopwatch.ElapsedMilliseconds >= nextAxisReadAtMs;
                            RobotSnapshot snapshot = await Task.Run(() => ReadRobotSnapshot(snpxRobot, diCount, doCount, readAxes), token);
                            token.ThrowIfCancellationRequested();
                            ApplyRobotSnapshot(snapshot, firstSnapshot);
                            firstSnapshot = false;
                            if (readAxes)
                            {
                                nextAxisReadAtMs = Math.Max(nextAxisReadAtMs + 100, stopwatch.ElapsedMilliseconds);
                            }

                            nextReadAtMs = Math.Max(nextReadAtMs + 100, stopwatch.ElapsedMilliseconds);
                            int delayMs = (int)Math.Max(0, nextReadAtMs - stopwatch.ElapsedMilliseconds);
                            if (delayMs > 0)
                            {
                                await Task.Delay(delayMs, token);
                            }
                        }
                    }
                    finally
                    {
                        if (connected)
                        {
                            await Task.Run(() => snpxRobot.Disconnect());
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested && !IsDisposed)
                {
                    AppendStatus("监听 RoboGuide 失败: " + ex.Message);
                }
            }
            finally
            {
                if (ReferenceEquals(_monitorCancellation, cancellation))
                {
                    _monitorCancellation = null;
                    if (!IsDisposed && !Disposing && !_changingMonitorState)
                    {
                        m_connect.Text = "Connect";
                        m_connect.Enabled = _initializedRobot != null && _diSignalTargets != null && _doSignalTargets != null;
                        m_robotPicked.Enabled = true;
                        m_Socketadd.Enabled = true;
                    }
                }

                cancellation.Dispose();
            }
        }

        private static RobotSnapshot ReadRobotSnapshot(FanucRobotClient snpxRobot, int diCount, int doCount, bool readAxes)
        {
            return new RobotSnapshot
            {
                AxisValues = readAxes ? CreateAxisValueList(snpxRobot.Position.ReadJointPosition(group: 1)) : null,
                DiValues = snpxRobot.DI.Read(1, diCount),
                DoValues = snpxRobot.DO.Read(1, doCount)
            };
        }

        private List<PlcSignalWrite> GetChangedPlcToRobotSignals()
        {
            var writes = new List<PlcSignalWrite>();
            for (int index = 0; index < _diSignalTargets.Length; index++)
            {
                bool value = _diSignalTargets[index].Value.BooleanValue;
                if (value != _lastPlcToRobotValues[index])
                {
                    writes.Add(new PlcSignalWrite
                    {
                        Index = index,
                        Address = DiSignals[index].Address,
                        Value = value
                    });
                }
            }

            return writes;
        }

        private static void WritePlcToRobotSignals(FanucRobotClient snpxRobot, IEnumerable<PlcSignalWrite> writes, CancellationToken token)
        {
            foreach (PlcSignalWrite write in writes)
            {
                token.ThrowIfCancellationRequested();
                if (!snpxRobot.DI.WriteSingle(write.Address, write.Value))
                {
                    throw new InvalidOperationException(string.Format("写入 RoboGuide DI[{0}] 失败。", write.Address));
                }
            }
        }

        private void ApplyRobotSnapshot(RobotSnapshot snapshot, bool synchronizeInitialValues)
        {
            if (snapshot.DiValues.Length != DiSignals.Count || snapshot.DoValues.Length != DoSignals.Count)
            {
                throw new InvalidOperationException("RoboGuide 返回的信号点位数量与初始化结果不一致。");
            }

            if (_diSignalTargets == null || _doSignalTargets == null || _lastPlcToRobotValues == null)
            {
                throw new InvalidOperationException("机器人信号表尚未初始化。");
            }

            bool displayChanged = snapshot.AxisValues != null;
            if (displayChanged)
            {
                RobotAxisValues = snapshot.AxisValues;
                UpdateRobotPose();
            }
            for (int index = 0; index < snapshot.DiValues.Length; index++)
            {
                RobotSignal signal = DiSignals[index];
                if (signal.Value != snapshot.DiValues[index])
                {
                    signal.Value = snapshot.DiValues[index];
                    DiSignals[index] = signal;
                }
            }

            displayChanged |= UpdateChangedSignals(DoSignals, _doSignalTargets, snapshot.DoValues, synchronizeInitialValues);
            if (displayChanged)
            {
                TxApplication.RefreshDisplay();
            }
        }

        private static bool UpdateChangedSignals(List<RobotSignal> signals, TxPlcSignalBase[] targets, bool[] values, bool synchronizeInitialValues)
        {
            bool changed = false;
            for (int index = 0; index < values.Length; index++)
            {
                RobotSignal signal = signals[index];
                if (signal.Value == values[index] && (!synchronizeInitialValues || targets[index].Value.BooleanValue == values[index]))
                {
                    continue;
                }

                targets[index].Value = new TxPlcValue { BooleanValue = values[index] };
                signal.Value = values[index];
                signals[index] = signal;
                changed = true;
            }

            return changed;
        }

        private static List<double> CreateAxisValueList(JointPosition jointPosition)
        {
            return new List<double>
            {
                jointPosition.J1,
                jointPosition.J2,
                jointPosition.J3,
                jointPosition.J4,
                jointPosition.J5,
                jointPosition.J6,
                jointPosition.J7,
                jointPosition.J8,
                jointPosition.J9
            };
        }

        private void UpdateRobotPose()
        {
            TxPoseData pose = new TxPoseData(robot.CurrentPose);
            var jointValues = pose.JointValues;
            for (int index = 0; index < Math.Min(jointValues.Count, RobotAxisValues.Count); index++)
            {
                jointValues[index] = RobotAxisValues[index] * Math.PI / 180.0;
            }

            pose.JointValues = jointValues;
            robot.CurrentPose = pose;
        }

        private void CreateRobotSignals()
        {
            var plcRoot = TxApplication.ActiveDocument.PlcProgramRootOrNull;
            var plcProgram = plcRoot == null ? null : plcRoot.CurrentPlcProgramOrNull;
            if (plcProgram == null)
            {
                throw new InvalidOperationException("当前文档尚未创建 PLC 程序，无法创建机器人信号。");
            }

            var existingFromRobot = new Dictionary<string, TxPlcFromRobotSignal>(StringComparer.OrdinalIgnoreCase);
            foreach (TxPlcFromRobotSignal signal in robot.FromRobotSignals)
            {
                if (!string.IsNullOrEmpty(signal.PrivateName))
                {
                    existingFromRobot[signal.PrivateName] = signal;
                }
            }

            var existingToRobot = new Dictionary<string, TxPlcToRobotSignal>(StringComparer.OrdinalIgnoreCase);
            foreach (TxPlcToRobotSignal signal in robot.ToRobotSignals)
            {
                if (!string.IsNullOrEmpty(signal.PrivateName))
                {
                    existingToRobot[signal.PrivateName] = signal;
                }
            }

            var plcFromRobot = new Dictionary<string, TxPlcFromRobotSignal>(StringComparer.OrdinalIgnoreCase);
            var plcToRobot = new Dictionary<string, TxPlcToRobotSignal>(StringComparer.OrdinalIgnoreCase);
            foreach (TxPlcSignalBase signal in plcProgram.GetSignals(new TxTypeFilter(new[] { typeof(TxPlcFromRobotSignal), typeof(TxPlcToRobotSignal) })))
            {
                var fromRobot = signal as TxPlcFromRobotSignal;
                if (fromRobot != null)
                {
                    plcFromRobot[fromRobot.Name] = fromRobot;
                }
                else
                {
                    var toRobot = signal as TxPlcToRobotSignal;
                    if (toRobot != null)
                    {
                        plcToRobot[toRobot.Name] = toRobot;
                    }
                }
            }

            var doTargets = new TxPlcSignalBase[DoSignals.Count];
            var diTargets = new TxPlcSignalBase[DiSignals.Count];
            var lastPlcToRobotValues = new bool[DiSignals.Count];
            for (int index = 0; index < DoSignals.Count; index++)
            {
                RobotSignal signal = DoSignals[index];
                string privateName = string.Format("DO[{0}]", signal.Address);
                string name = string.Format("{0}_DO_{1}", robot.Name, signal.Address);
                TxPlcFromRobotSignal existing;
                if (existingFromRobot.TryGetValue(privateName, out existing) || plcFromRobot.TryGetValue(name, out existing))
                {
                    existing.Value = new TxPlcValue { BooleanValue = signal.Value };
                    doTargets[index] = existing;
                    continue;
                }

                var data = new TxPlcFromRobotSignalCreationData(name)
                {
                    PrivateName = privateName,
                    TargetRobot = robot,
                    Comment = signal.Comment ?? string.Empty,
                    DataType = TxPlcSignalDataType.Bool,
                    Address = new TxPlcSignalAddress((signal.Address - 1) / 8, (signal.Address - 1) % 8)
                };
                TxPlcFromRobotSignal created = plcProgram.CreateFromRobotSignal(data);
                created.Value = new TxPlcValue { BooleanValue = signal.Value };
                existingFromRobot.Add(privateName, created);
                doTargets[index] = created;
            }

            for (int index = 0; index < DiSignals.Count; index++)
            {
                RobotSignal signal = DiSignals[index];
                string privateName = string.Format("DI[{0}]", signal.Address);
                string name = string.Format("{0}_DI_{1}", robot.Name, signal.Address);
                TxPlcToRobotSignal existing;
                if (existingToRobot.TryGetValue(privateName, out existing) || plcToRobot.TryGetValue(name, out existing))
                {
                    existing.Value = new TxPlcValue { BooleanValue = signal.Value };
                    diTargets[index] = existing;
                    lastPlcToRobotValues[index] = signal.Value;
                    continue;
                }

                var data = new TxPlcToRobotSignalCreationData(name)
                {
                    PrivateName = privateName,
                    Comment = signal.Comment ?? string.Empty,
                    DataType = TxPlcSignalDataType.Bool,
                    Address = new TxPlcSignalAddress((signal.Address - 1) / 8, (signal.Address - 1) % 8)
                };
                TxPlcToRobotSignal created = plcProgram.CreateToRobotSignal(data);
                robot.AddToRobotSignal(created);
                created.Value = new TxPlcValue { BooleanValue = signal.Value };
                existingToRobot.Add(privateName, created);
                diTargets[index] = created;
                lastPlcToRobotValues[index] = signal.Value;
            }

            _diSignalTargets = diTargets;
            _doSignalTargets = doTargets;
            _lastPlcToRobotValues = lastPlcToRobotValues;
        }

        private static List<RobotSignal> CreateSignalList(FanucRobotClient snpxRobot, string signalType, IList<bool> values)
        {
            var signals = new List<RobotSignal>(values.Count);
            for (int index = 0; index < values.Count; index++)
            {
                int address = index + 1;
                string comment = ReadSignalComment(snpxRobot, signalType, address);
                signals.Add(new RobotSignal
                {
                    Address = address,
                    Name = string.IsNullOrWhiteSpace(comment)
                        ? string.Format("{0}[{1}]", signalType, address)
                        : comment,
                    Comment = comment,
                    Value = values[index]
                });
            }

            return signals;
        }

        private static string ReadSignalComment(FanucRobotClient snpxRobot, string signalType, int address)
        {
            try
            {
                return snpxRobot.Comment.Read(signalType, address);
            }
            catch
            {
                return null;
            }
        }
    }
}
