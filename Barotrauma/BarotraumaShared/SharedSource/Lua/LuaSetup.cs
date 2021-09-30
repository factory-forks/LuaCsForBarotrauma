﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Barotrauma.Networking;
using MoonSharp.Interpreter;
using Microsoft.Xna.Framework;
using System.Threading.Tasks;
using Barotrauma.Items.Components;
using System.Diagnostics;
using System.Linq;
using MoonSharp.Interpreter.Interop;
using System.Reflection;

namespace Barotrauma
{
	partial class LuaSetup
	{
		public static LuaSetup luaSetup;

		public Script lua;

		public LuaHook hook;
		public LuaGame game;
		public LuaNetworking networking;

		public LuaScriptLoader luaScriptLoader;

		public void HandleLuaException(Exception ex)
		{
			if (ex is InterpreterException)
			{
				if (((InterpreterException)ex).DecoratedMessage == null)
					PrintMessage(((InterpreterException)ex).Message);
				else
					PrintMessage(((InterpreterException)ex).DecoratedMessage);
			}
			else
			{
				PrintMessage(ex.ToString());
			}
		}

		public void PrintMessage(object message)
		{
			if (message == null) { message = "nil"; }
			string str = message.ToString();

			Console.WriteLine(str);

			for (int i = 0; i < str.Length; i += 1024)
			{
				string subStr = str.Substring(i, Math.Min(1024, str.Length - i));


#if SERVER
				if (GameMain.Server != null)
				{
					foreach (var c in GameMain.Server.ConnectedClients)
					{
						GameMain.Server.SendDirectChatMessage(subStr, c, ChatMessageType.Console);
					}

					GameServer.Log("[LUA] " + subStr, ServerLog.MessageType.ServerMessage);
				}
#else
			DebugConsole.NewMessage("[LUA] " + message.ToString());
#endif
			}

		}

		public void PrintMessageNoLog(object message)
		{
			if (message == null) { message = "nil"; }
			Console.WriteLine(message.ToString());
		}

		public DynValue DoString(string code, Table globalContext = null, string codeStringFriendly = null)
		{
			try
			{
				return lua.DoString(code, globalContext, codeStringFriendly);
			}
			catch (Exception e)
			{
				HandleLuaException(e);
			}

			return null;
		}

		public DynValue DoFile(string file, Table globalContext = null, string codeStringFriendly = null)
		{
			try
			{
				return lua.DoFile(file, globalContext, codeStringFriendly);

			}
			catch (Exception e)
			{
				HandleLuaException(e);
			}

			return null;
		}


		public DynValue LoadString(string file, Table globalContext = null, string codeStringFriendly = null)
		{
			try
			{
				return lua.LoadString(file, globalContext, codeStringFriendly);

			}
			catch (Exception e)
			{
				HandleLuaException(e);
			}

			return null;
		}

		public DynValue LoadFile(string file, Table globalContext = null, string codeStringFriendly = null)
		{
			try
			{
				return lua.LoadFile(file, globalContext, codeStringFriendly);

			}
			catch (Exception e)
			{
				HandleLuaException(e);
			}

			return null;
		}

		public DynValue Require(string modname, Table globalContext)
		{
			try
			{
				return lua.Call(lua.RequireModule(modname, globalContext));

			}
			catch (Exception e)
			{
				HandleLuaException(e);
			}

			return null;
		}

		public static DynValue CreateUserDataSafe(object o)
		{
			if (o == null)
				return DynValue.Nil;

			return UserData.Create(o);

		}


		public object CallFunction(object function, object[] arguments)
		{
			return lua.Call(function, arguments);
		}

		public void SetModulePaths(string[] str)
		{
			luaScriptLoader.ModulePaths = str;
		}

		public float TestFunction(float value)
		{
			return value * 2;
		}

		// messy solution
		private object HandleCall(object arg1, ScriptExecutionContext arg2, CallbackArguments arg3)
		{
			var what = arg3.RawGet(0, true);

			var code = "return " + what.UserData.Descriptor.Type.Name + ".__new(";

			var tbl = new Table(lua);
			tbl[what.UserData.Descriptor.Type.Name] = what;
			
			for(var i=1; i < arg3.Count; i++)
			{
				if (i == arg3.Count - 1)
					code = code + "arg" + i;
				else
					code = code + "arg" + i + ",";

				tbl["arg" + i] = arg3.RawGet(i, true);
			}

			code = code + ")";

			try
			{
				return lua.DoString(code, tbl);
			}
			catch(Exception e)
			{
				HandleLuaException(e);
			}

			return null;
		}

		private void AddCallMetaMember(IUserDataDescriptor IUDD)
		{
			var descriptor = (StandardUserDataDescriptor)IUDD;
			descriptor.RemoveMetaMember("__call");
			descriptor.AddMetaMember("__call", new ObjectCallbackMemberDescriptor("__call", HandleCall));
		}

		public void Stop()
		{
			hook = new LuaHook(null);
			game = new LuaGame(null);
			networking = new LuaNetworking(null);
			luaScriptLoader = null;

			luaSetup = null;
		}

		public void Initialize()
		{
			luaSetup = this;

			PrintMessage("Lua!");

			luaScriptLoader = new LuaScriptLoader(this);
			luaScriptLoader.ModulePaths = new string[] { };

			LuaCustomConverters.RegisterAll();

			UserData.RegisterType<TraitorMessageType>();
			UserData.RegisterType<JobPrefab>();
			UserData.RegisterType<Job>();
			UserData.RegisterType<Rectangle>();
			UserData.RegisterType<Point>();
			UserData.RegisterType<Level.InterestingPosition>();
			UserData.RegisterType<Level.PositionType>();
			UserData.RegisterType<Level>();
			UserData.RegisterType<Items.Components.Steering>();
			UserData.RegisterType<ServerLog.MessageType>();
			UserData.RegisterType<SpawnType>();
			UserData.RegisterType<ChatMessageType>();
			UserData.RegisterType<WayPoint>();
			UserData.RegisterType<Character>();
			UserData.RegisterType<Item>();
			UserData.RegisterType<Submarine>();
			UserData.RegisterType<Client>();
			UserData.RegisterType<LuaPlayer>();
			UserData.RegisterType<LuaHook>();
			UserData.RegisterType<LuaGame>();
			UserData.RegisterType<LuaRandom>();
			UserData.RegisterType<LuaTimer>();
			UserData.RegisterType<LuaFile>();
			UserData.RegisterType<LuaNetworking>();
			UserData.RegisterType<CauseOfDeathType>();
			UserData.RegisterType<AfflictionPrefab>();
			UserData.RegisterType<Affliction>();
			UserData.RegisterType<CharacterHealth>();
			UserData.RegisterType<AnimController>();
			UserData.RegisterType<Limb>();
			UserData.RegisterType<Ragdoll>();
			UserData.RegisterType<ChatMessage>();
			UserData.RegisterType<CharacterHealth.LimbHealth>();
			UserData.RegisterType<InputType>();
			UserData.RegisterType<AttackResult>();
			UserData.RegisterType<Entity>();
			UserData.RegisterType<MapEntity>();
			UserData.RegisterType<MapEntityPrefab>();
			UserData.RegisterType<CauseOfDeath>();
			UserData.RegisterType<CharacterTeamType>();
			UserData.RegisterType<Connection>();
			UserData.RegisterType<ItemComponent>();
			UserData.RegisterType<WifiComponent>();
			UserData.RegisterType<LightComponent>();
			UserData.RegisterType<Holdable>();
			UserData.RegisterType<CustomInterface>();
			UserData.RegisterType<Inventory>();
			UserData.RegisterType<ItemContainer>();
			UserData.RegisterType<PowerContainer>();
			UserData.RegisterType<Pickable>();
			UserData.RegisterType<Reactor>();
			UserData.RegisterType<CharacterInventory>();
			UserData.RegisterType<Hull>();
			UserData.RegisterType<Gap>();
			UserData.RegisterType<PhysicsBody>();
			UserData.RegisterType<SubmarineBody>();
			UserData.RegisterType<InvSlotType>();
			UserData.RegisterType<ItemPrefab>();
			UserData.RegisterType<SerializableProperty>();
			UserData.RegisterType<StatusEffect>();
			UserData.RegisterType<CustomInterface.CustomInterfaceElement>();
			UserData.RegisterType<FireSource>();
			UserData.RegisterType<Fabricator>();
			UserData.RegisterType<Pair<JobPrefab, int>>();
			UserData.RegisterType<ContentPackage>();
			UserData.RegisterType<SubmarineInfo>();
			UserData.RegisterType<SubmarineBody>();
			UserData.RegisterType<Explosion>();
			UserData.RegisterType<AIController>();
			UserData.RegisterType<EnemyAIController>();
			UserData.RegisterType<HumanAIController>();
			UserData.RegisterType<AICharacter>();
			UserData.RegisterType<AITarget>();
			UserData.RegisterType<AITargetMemory>();
			UserData.RegisterType<ServerSettings>();
			UserData.RegisterType<PrefabCollection<ItemPrefab>>();
			UserData.RegisterType<PrefabCollection<JobPrefab>>();
			UserData.RegisterType<GameSession>();
			UserData.RegisterType<CampaignMode>();
			UserData.RegisterType<InputType>();
			UserData.RegisterType<Key>();
			UserData.RegisterType<NetLobbyScreen>();
			UserData.RegisterType<IWriteMessage>();
			UserData.RegisterType<IReadMessage>();
			UserData.RegisterType<ServerPacketHeader>();
			UserData.RegisterType<ClientPacketHeader>();
			UserData.RegisterType<DeliveryMethod>();
			UserData.RegisterType<RelayComponent>();
			UserData.RegisterType<MemoryComponent>();

			AddCallMetaMember(UserData.RegisterType<Vector2>());
			AddCallMetaMember(UserData.RegisterType<Vector3>());
			AddCallMetaMember(UserData.RegisterType<Vector4>());
			AddCallMetaMember(UserData.RegisterType<CharacterInfo>());
			AddCallMetaMember(UserData.RegisterType<Signal>());
			AddCallMetaMember(UserData.RegisterType<Color>());
			AddCallMetaMember(UserData.RegisterType<Point>());

#if SERVER

#elif CLIENT
			UserData.RegisterType<LuaGUI>();
			UserData.RegisterType<ChatBox>();
			UserData.RegisterType<Anchor>();

			AddCallMetaMember(UserData.RegisterType<GUILayoutGroup>());
			AddCallMetaMember(UserData.RegisterType<GUITextBox>());
			AddCallMetaMember(UserData.RegisterType<GUIButton>());
			AddCallMetaMember(UserData.RegisterType<RectTransform>());
			AddCallMetaMember(UserData.RegisterType<GUIFrame>());
#endif
			lua = new Script(CoreModules.Preset_SoftSandbox);

			lua.Options.DebugPrint = PrintMessage;

			lua.Options.ScriptLoader = luaScriptLoader;

			hook = new LuaHook(this);
			game = new LuaGame(this);
			networking = new LuaNetworking(this);

			lua.Globals["TestFunction"] = (Func<float, float>)TestFunction;

			lua.Globals["printNoLog"] = (Action<object>)PrintMessageNoLog;

			lua.Globals["dofile"] = (Func<string, Table, string, DynValue>)DoFile;
			lua.Globals["loadfile"] = (Func<string, Table, string, DynValue>)LoadFile;
			lua.Globals["require"] = (Func<string, Table, DynValue>)Require;

			lua.Globals["dostring"] = (Func<string, Table, string, DynValue>)DoString;
			lua.Globals["load"] = (Func<string, Table, string, DynValue>)LoadString;

			lua.Globals["setmodulepaths"] = (Action<string[]>)SetModulePaths;

			lua.Globals["Player"] = new LuaPlayer();
			lua.Globals["Game"] = game;
			lua.Globals["Hook"] = hook;
			lua.Globals["Random"] = new LuaRandom();
			lua.Globals["Timer"] = new LuaTimer(this);
			lua.Globals["File"] = UserData.CreateStatic<LuaFile>();
			lua.Globals["Networking"] = networking;
			lua.Globals["WayPoint"] = UserData.CreateStatic<WayPoint>();
			lua.Globals["SpawnType"] = UserData.CreateStatic<SpawnType>();
			lua.Globals["ChatMessageType"] = UserData.CreateStatic<ChatMessageType>();
			lua.Globals["ServerLog_MessageType"] = UserData.CreateStatic<ServerLog.MessageType>();
			lua.Globals["Submarine"] = UserData.CreateStatic<Submarine>();
			lua.Globals["Client"] = UserData.CreateStatic<Client>();
			lua.Globals["Character"] = UserData.CreateStatic<Character>();
			lua.Globals["CharacterInfo"] = UserData.CreateStatic<CharacterInfo>();
			lua.Globals["Item"] = UserData.CreateStatic<Item>();
			lua.Globals["ItemPrefab"] = UserData.CreateStatic<ItemPrefab>();
			lua.Globals["Level"] = UserData.CreateStatic<Level>();
			lua.Globals["PositionType"] = UserData.CreateStatic<Level.PositionType>();
			lua.Globals["JobPrefab"] = UserData.CreateStatic<JobPrefab>();
			lua.Globals["TraitorMessageType"] = UserData.CreateStatic<TraitorMessageType>();
			lua.Globals["CauseOfDeathType"] = UserData.CreateStatic<CauseOfDeathType>();
			lua.Globals["AfflictionPrefab"] = UserData.CreateStatic<AfflictionPrefab>();
			lua.Globals["CharacterTeamType"] = UserData.CreateStatic<CharacterTeamType>();
			lua.Globals["Vector2"] = UserData.CreateStatic<Vector2>();
			lua.Globals["Vector3"] = UserData.CreateStatic<Vector3>();
			lua.Globals["Vector4"] = UserData.CreateStatic<Vector4>();
			lua.Globals["Color"] = UserData.CreateStatic<Color>();
			lua.Globals["Point"] = UserData.CreateStatic<Point>();
			lua.Globals["ChatMessage"] = UserData.CreateStatic<ChatMessage>();
			lua.Globals["Hull"] = UserData.CreateStatic<Hull>();
			lua.Globals["InvSlotType"] = UserData.CreateStatic<InvSlotType>();
			lua.Globals["Gap"] = UserData.CreateStatic<Gap>();
			lua.Globals["ContentPackage"] = UserData.CreateStatic<ContentPackage>();
			lua.Globals["ClientPermissions"] = UserData.CreateStatic<ClientPermissions>();
			lua.Globals["Signal"] = UserData.CreateStatic<Signal>();
			lua.Globals["DeliveryMethod"] = UserData.CreateStatic<DeliveryMethod>();
			lua.Globals["ClientPacketHeader"] = UserData.CreateStatic<ClientPacketHeader>();
			lua.Globals["ServerPacketHeader"] = UserData.CreateStatic<ServerPacketHeader>();
			
#if SERVER

#elif CLIENT
			lua.Globals["GUI"] = new LuaGUI(this);
#endif


			// obsolete
			lua.Globals["CreateVector2"] = (Func<float, float, Vector2>)CreateVector2;
			lua.Globals["CreateVector3"] = (Func<float, float, float, Vector3>)CreateVector3;
			lua.Globals["CreateVector4"] = (Func<float, float, float, float, Vector4>)CreateVector4;

			bool isServer = true;

#if SERVER
			isServer = true;
#else
			isServer = false;
#endif

			lua.Globals["SERVER"] = isServer;
			lua.Globals["CLIENT"] = !isServer;
			
			if (File.Exists("Lua/MoonsharpSetup.lua")) // try the default loader
				DoFile("Lua/MoonsharpSetup.lua");
			else if (File.Exists("Mods/LuaForBarotrauma/Lua/MoonsharpSetup.lua")) // in case its the workshop version
				DoFile("Mods/LuaForBarotrauma/Lua/MoonsharpSetup.lua");
			else // fallback to c# script loading
			{
				List<string> modulePaths = new List<string>();

				foreach (string d in Directory.GetDirectories("Mods"))
				{
					modulePaths.Add(d + "/Lua/?.lua");

					if (Directory.Exists(d + "/Lua/Autorun"))
					{
						luaScriptLoader.RunFolder(d + "/Lua/Autorun");
					}
				}

				luaScriptLoader.ModulePaths = modulePaths.ToArray();
			}
		}

		public LuaSetup()
		{
			hook = new LuaHook(null);
			game = new LuaGame(null);
			networking = new LuaNetworking(null);
		}

	}



}
