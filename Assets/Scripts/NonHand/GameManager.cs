// /// <summary>
// /// GameManager là "Tổng quản" điều phối toàn bộ luật chơi và trạng thái trận đấu.
// /// 
// /// Tác dụng:
// /// - Quản lý luật thắng thua: Kiểm tra liên tục xem phe nào thắng (Dựa trên số người sống sót hoặc tiến độ làm nhiệm vụ).
// /// - Điều khiển quy trình game: Chuyển đổi giữa các giai đoạn: Chờ ở sảnh -> Chơi -> Họp/Vote -> Kết thúc.
// /// - Đồng bộ cài đặt (Settings): Đảm bảo tất cả người chơi đều có cùng tốc độ chạy, thời gian vote, số lượng nhiệm vụ...
// /// 
// /// Nếu thiếu script này:
// /// - Game "vô luật pháp": Giết hết người cũng không thắng, làm xong nhiệm vụ cũng không end game.
// /// - Mạnh ai nấy chạy: Không có ai đứng ra tổ chức họp hành hay vote đuổi người.
// /// - Game không thể bắt đầu (Start) hoặc kết thúc (End).
// /// </summary>

// using Fusion;
// using Fusion.Sockets;
// using Helpers.Bits;
// using System;
// using System.Collections.Generic;
// using UnityEngine;

// namespace NonHand
// {
//     /// <summary>
//     /// Manager that handles the main gameplay loop of Impostor.
//     /// </summary>
//     public class GameManager : NetworkBehaviour, INetworkRunnerCallbacks
//     {
//         public static GameManager Instance { get; private set; }
//         public static GameState State { get; private set; }
//         public static ResourcesManager rm { get; private set; }
//         public static InterfaceManager im { get; private set; }
//         public static VoiceManager vm { get; private set; }

//         public FusionBootstrap starter;

//         public UIScreen pauseScreen;
//         public UIScreen optionsScreen;

//         public MapData preGameMapData;

//         public MapData mapData;

//         [Networked]
//         public PlayerObject MeetingCaller { get; set; }

//         [Networked]
//         public PlayerObject MeetingContext { get; set; }

//         [Networked]
//         public PlayerObject VoteResult { get; set; }

//         [Networked, OnChangedRender(nameof(GameSettingsChanged))]
//         public GameSettings Settings { get; set; } = GameSettings.Default;

//         [Networked, OnChangedRender(nameof(TasksCompletedChanged))]
//         public byte TasksCompleted { get; set; }

//         public List<TaskStation> taskDisplayList;
//         public readonly Dictionary<TaskBase, byte> taskDisplayAmounts = new Dictionary<TaskBase, byte>();

//         [Networked, Tooltip("Is the meeting screen currently active.")]
//         public NetworkBool MeetingScreenActive { get; set; }

//         [Networked, Tooltip("Is the vocting screen currently active.")]
//         public NetworkBool VotingScreenActive { get; set; }

//         private void Awake()
//         {
//             if(Instance == null)
//             {
//                 Instance = this;
//                 rm = GetComponent<ResourcesManager>();
//                 im = GetComponent<InterfaceManager>();
//                 vm = GetComponent<VoiceManager>();
//                 State = GetComponent<GameState>();
//             }
//             else
//             {
//                 Destroy(gameObject);
//             }
//         }

//         public override void Spawned()
//         {
//             base.Spawned();
//             vm.Init(
//                 Runner.GetComponent<Photon.Voice.Unity.VoiceConnection>(),
//                 Runner.GetComponentInChildren<Photon.Voice.Unity.Recorder>()
//             );

//             Runner.AddCallbacks(this);

//             UpdateGameSettingHUD(Settings);
//         }

//         public override void Despawned(NetworkRunner runner, bool hasState)
//         {
//             base.Despawned(runner, hasState);
//             runner.RemoveCallbacks(this);
//             starter.Shutdown();
//         }

//         public override void Render()
//         {
//             // Updates the displays ping ever 100 ticks.
//             if (Runner.Tick % 100 == 0)
//                 im.gameUI.pingText.text = $"Ping: {1000 * Runner.GetPlayerRtt(Runner.LocalPlayer):N0}ms";
//         }

//         public void Server_StartGame()
//         {
//             if (State.ActiveState is not PregameStateBehaviour) return;

//             if (PlayerRegistry.Count < 4)
//             {
//                 Debug.LogWarning($"It's recommended to play with at least 4 people!");
//             }

//             State.Server_SetState<PlayStateBehaviour>();
//         }

//         /// <summary>
//         /// Method called when a player has murdered, not ejected from a vote.
//         /// </summary>
//         public static void OnPlayerKilled()
//         {
//             int numCrew = PlayerRegistry.CountWhere(p => !p.Controller.IsDead && p.Controller.IsSuspect == false);
//             int numSus = PlayerRegistry.CountWhere(p => !p.Controller.IsDead && p.Controller.IsSuspect == true);

//             if (numCrew <= numSus)
//             {
//                 State.winState.crewWin = false;
//                 State.Server_DelaySetState<WinStateBehaviour>(1);
//             }
//         }

//         public void CallMeeting(PlayerRef source, NetworkObject context, Tick tick)
//         {
//             // Only the state authority can call this.
//             if (!HasStateAuthority)
//                 return;

//             PlayerObject caller = PlayerRegistry.GetPlayer(source);

//             if (context == null)
//             {
//                 if (caller.Controller.EmergencyMeetingUses > 0)
//                 {
//                     caller.Controller.EmergencyMeetingUses--;
//                     MeetingContext = null;
//                     MeetingCaller = caller;
//                     State.Server_SetState<MeetingStateBehaviour>();

//                     im.gameUI.meetingUI.Server_SetTimer(tick);
//                 }
//                 else
//                 {
//                     Debug.Log($"{caller.Nickname} is out of emergency meeting calls");
//                 }
//             }
//             else if (context.TryGetBehaviour(out DeadPlayer body))
//             {
//                 MeetingContext = PlayerRegistry.GetPlayer(body.Ref);
//                 MeetingCaller = caller;

//                 State.Server_SetState<MeetingStateBehaviour>();

//                 im.gameUI.meetingUI.Server_SetTimer(tick);
//                 
//                 // Only the state authority can despawn objects.
//                 if (HasStateAuthority)
//                     Runner.Despawn(body.Object);
//             }
//         }

//         [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
//         void Rpc_CompleteTask(PlayerRef player, TaskStation completedTask)
//         {
//             // Removes the task from the complete players list
//             PlayerRegistry.GetPlayer(player).Controller.tasks.Remove(completedTask);

//             TasksCompleted++;

//             if (TasksCompleted == Settings.numTasks * (PlayerRegistry.Count - Settings.numImposters))
//             {
//                 Debug.Log("All Tasks Completed - Crew Wins");
//                 State.winState.crewWin = true;
//                 State.Server_SetState<WinStateBehaviour>();
//             }
//             else
//             {
//                 Debug.Log($"{TasksCompleted} tasks completed");
//             }
//         }

//         public void CompleteTask()
//         {
//             TaskStation task = PlayerMovement.Local.activeInteractable as TaskStation;
//             Debug.LogWarning(task);
//             if (taskDisplayList.Remove(task))
//             {
//                 im.gameUI.UpdateTaskUI();
//                 Rpc_CompleteTask(Runner.LocalPlayer, task);
//             }
//         }

//         public int TaskCount(TaskBase taskBase)
//         {
//             int count = 0;
//             for (int i = 0; i < taskDisplayList.Count; i++)
//             {
//                 if (taskDisplayList[i].taskUI.task == taskBase)
//                     count++;
//             }
//             return count;
//         }

//         public List<string> TaskNames()
//         {
//             List<string> taskNameList = new List<string>();
//             for (int i = 0, len = taskDisplayList.Count; i < len; i++)
//             {
//                 taskNameList.Add(taskDisplayList[i].taskUI.task.Name);
//             }

//             return taskNameList;
//         }

//         public static void QuitGame()
//         {
//     #if UNITY_EDITOR
//             UnityEditor.EditorApplication.isPlaying = false;
//     #else
//             Application.Quit();
//     #endif
//         }

//         void UpdateGameSettingHUD(GameSettings settings)
//         {
//             im.imposterCountDisplay.text = $"{settings.numImposters}";
//             im.tasksCountDisplay.text = $"{settings.numTasks}";
//             im.emergencyMeetingsDisplay.text = $"{settings.numEmergencyMeetings}";
//             im.discussionTimeDisplay.text = $"{settings.discussionTime}s";
//             im.votingTimeDisplay.text = $"{settings.votingTime}s";
//             im.walkSpeedDisplay.text = $"{settings.walkSpeed}";
//             im.playerCollisionDisplay.text = settings.playerCollision ? "Yes" : "No";
//         }

//         void GameSettingsChanged()
//         {
//             GameSettings settings = Settings;

//             UpdateGameSettingHUD(settings);

//             if (Instance.Runner.IsServer)
//             {
//                 int playerLayer = LayerMask.NameToLayer("Player");
//                 PlayerRegistry.ForEach(p =>
//                 {
//                     p.Controller.cc.SetCollisionLayerMask(p.Controller.cc.Settings.CollisionLayerMask.value.OverrideBit(playerLayer, settings.playerCollision));
//                     p.Controller.Speed = settings.walkSpeed;
//                 });
//             }
//         }

//         void TasksCompletedChanged()
//         {
//             GameManager self = this;
//             float pct = self.TasksCompleted /
//                 (float)(self.Settings.numTasks * (PlayerRegistry.Count - Instance.Settings.numImposters));

//             im.gameUI.totalTaskBarFill.fillAmount = pct;
//         }

//         void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
//         {
//             UIScreen.CloseAll();
//         }

//         void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, Fusion.Sockets.NetAddress remoteAddress, Fusion.Sockets.NetConnectFailedReason reason) { }
//         void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
//         void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
//         void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
//         void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
//         void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
//         void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
//         void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
//         void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
//         void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
//         void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
//         void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
//         void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
//         void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
//         void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
//         void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
//         void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
//         void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
//     }
// }