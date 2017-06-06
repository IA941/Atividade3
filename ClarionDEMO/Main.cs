
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using WorldServerLibrary;
using WorldServerLibrary.Model;
using WorldServerLibrary.Exceptions;
using Gtk;

namespace ClarionDEMO
{
	class MainClass
	{
		#region properties
		private WorldServer worldServer = null;
        private ClarionAgent agent;
        String creatureId = String.Empty;
        String creatureName = String.Empty;
		#endregion

		#region constructor
		public MainClass() {
			Application.Init();
			Console.WriteLine ("Clarion Demo V0.7");
			try
            {
                worldServer = new WorldServer("localhost", 4011);

                String message = worldServer.Connect();

                if (worldServer != null && worldServer.IsConnected)
                {
                    Console.Out.WriteLine("[SUCCESS] " + message + "\n");

                    //worldServer.SendWorldReset();

                    worldServer.NewCreature(400, 200, 0, out creatureId, out creatureName);
                    worldServer.SendCreateLeaflet();
                    // worldServer.NewBrick(4, 747, 2, 800, 567);
                    // worldServer.NewBrick(4, 50, -4, 747, 47);
                    // worldServer.NewBrick(4, 49, 562, 796, 599);
                    // worldServer.NewBrick(4, -2, 6, 50, 599);

                    //worldServer.NewBrick(4, 800, -5, 800, 600);
                    //worldServer.NewBrick(4, 3, -4, 800, 1);
                    //worldServer.NewBrick(4, 5, 600, 800, 600);
                    //worldServer.NewBrick(4, 1, 2, 4, 600);

                    worldServer.thriveWorld();

                    if (!String.IsNullOrWhiteSpace(creatureId))
                    {
                        worldServer.SendStartCamera(creatureId);
                        worldServer.SendStartCreature(creatureId);
                    }

                    Console.Out.WriteLine("Creature created with name: " + creatureId + "\n");
					agent = new ClarionAgent(worldServer,creatureId,creatureName);
                    agent.Run();
					Console.Out.WriteLine("Running Simulation ...\n");
                }
            }
            catch (WorldServerInvalidArgument invalidArtgument)
            {
                Console.Out.WriteLine(String.Format("[ERROR] Invalid Argument: {0}\n", invalidArtgument.Message));
            }
            catch (WorldServerConnectionError serverError)
            {
                Console.Out.WriteLine(String.Format("[ERROR] Is is not possible to connect to server: {0}\n", serverError.Message));
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(String.Format("[ERROR] Unknown Error: {0}\n", ex.Message));
            }
			Application.Run();
		}
		#endregion

		#region Methods
		public static void Main (string[] args)	{
			new MainClass();
		}
			
        #endregion
	}
}
