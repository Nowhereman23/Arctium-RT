using Framework.Console;
using Framework.Console.Commands;
using Framework.Database;
using Framework.Logging;
using Framework.ObjectDefines;
using RealmServer.Network;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace RealmServer
{
    public class RealmServer
    {
        public static void InitializeRealms()
        {
            RealmClass.realm = new RealmNetwork();

            // Add realms from database.
            Log.Message(LogType.NORMAL, "Updating Realm List...");
            Log.Message();

            SQLResult result = DB.Realms.Select("SELECT * FROM realms");
            var res = result.Read("id", "name", "ip", "port");

            for (int i = 0; i < result.Count; i++)
            {
                RealmClass.Realms.Add(new Realm()
                {
                    Id = result.Read<uint>("id"),
                    Name = result.Read<string>("name"),
                    IP = result.Read<string>("ip"),
                    Port = result.Read<uint>("port"),
                });

                Log.Message(LogType.NORMAL, "Added Realm \"{0}\"", RealmClass.Realms[i].Name);
            }

            Log.Message();

            RealmClass.realm.Start("127.0.0.1", 3724);
            if (RealmClass.realm.IsWorking)
            {
                Log.Message(LogType.NORMAL, "RealmServer listening on {0} port {1}.", "127.0.0.1", 3724);
                Log.Message(LogType.NORMAL, "RealmServer successfully started!");
            }
            else
            {
                Log.Message(LogType.ERROR, "RealmServer couldn't be started: ");
            }
        }

        public static void InitializeDatabase()
        {
            DB.Realms.Init("realmdb.sqlite");
        }

        public static void InitializeLogSystem(ref TextBlock block)
        {
            Log.CurrentBlock = block;
        }

        public static void InitializeCommandDefinitions()
        {
            CommandDefinitions.Initialize();
        }

        public static void RunCommand(ref TextBox input, KeyRoutedEventArgs e)
        {
            CommandManager.InitCommands(input, e.Key);
        }
    }
}
