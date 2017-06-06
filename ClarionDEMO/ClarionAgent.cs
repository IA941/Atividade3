using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Clarion;
using Clarion.Framework;
using Clarion.Framework.Core;
using Clarion.Framework.Templates;
using WorldServerLibrary.Model;
using WorldServerLibrary;
using System.Threading;
using Gtk;

namespace ClarionDEMO
{
    /// <summary>
    /// Public enum that represents all possibilities of agent actions
    /// </summary>
    public enum CreatureActions
    {
        DO_NOTHING,
        ROTATE_CLOCKWISE,
        GO_AHEAD,
        SACK,
        EAT,
        HIDE,
        STOP
    }

    public class ClarionAgent
    {
        #region Constants
        /// <summary>
        /// Constant that represents the Visual Sensor
        /// </summary>
        private String SENSOR_VISUAL_DIMENSION = "VisualSensor";
        /// <summary>
        /// Constant that represents that there is at least one wall ahead
        /// </summary>
        private String DIMENSION_WALL_AHEAD = "WallAhead";
        /// <summary>
        /// Constant that represents that there is a food ahead
        /// </summary>
        private String DIMENSION_FOOD_AHEAD = "FoodAhead";
        /// <summary>
        /// Constant that represents that there is a leaflet jewel ahead
        /// </summary>
        private String DIMENSION_LEAFLET_JEWEL_AHEAD = "LeafletJewelAhead";
        /// <summary>
        /// Constant that represents that there is a non-leaflet jewel ahead
        /// </summary>
        private String DIMENSION_NON_LEAFLET_JEWEL_AHEAD = "NonLeafletJewelAhead";
        /// <summary>
        /// Constant that represents that there is an object in close distance
        /// </summary>
        private String DIMENSION_CLOSE_OBJECT_AHEAD = "CloseObjectAhead";
        /// <summary>
        /// Constant that represents that there is an object in close distance
        /// </summary>
        private String DIMENSION_HAS_COMPLETED_LEAFLET = "HasCompletedLeaflet";
        double prad = 0;
        #endregion

        #region Properties
		public Mind mind;
		String creatureId = String.Empty;
		String creatureName = String.Empty;
        Thing targetThing = null;
        #region Simulation
        /// <summary>
        /// If this value is greater than zero, the agent will have a finite number of cognitive cycle. Otherwise, it will have infinite cycles.
        /// </summary>
        public double MaxNumberOfCognitiveCycles = -1;
        /// <summary>
        /// Current cognitive cycle number
        /// </summary>
        private double CurrentCognitiveCycle = 0;
        /// <summary>
        /// Time between cognitive cycle in miliseconds
        /// </summary>
        public Int32 TimeBetweenCognitiveCycles = 0;
        /// <summary>
        /// A thread Class that will handle the simulation process
        /// </summary>
        private Thread runThread;
        #endregion

        #region Agent
		private WorldServer worldServer;
        /// <summary>
        /// The agent 
        /// </summary>
        private Clarion.Framework.Agent CurrentAgent;
        #endregion

        #region Perception Input
        /// <summary>
        /// Perception input to indicates a wall ahead
        /// </summary>
		private DimensionValuePair inputWallAhead;
        /// <summary>
        /// Perception input to indicates a food ahead
        /// </summary>
		private DimensionValuePair inputFoodAhead;
        /// <summary>
        /// Perception input to indicates a leaflet jewel ahead
        /// </summary>
		private DimensionValuePair inputLeafletJewelAhead;
        /// <summary>
        /// Perception input to indicates a non-leaflet jewel ahead
        /// </summary>
		private DimensionValuePair inputNonLeafletJewelAhead;
        /// <summary>
        /// Perception input to indicates a close object ahead
        /// </summary>
		private DimensionValuePair inputCloseObjectAhead;
        /// <summary>
        /// Perception input to indicates the leaflet is complete
        /// </summary>
		private DimensionValuePair inputHasCompletedLeaflet;
        #endregion

        #region Action Output
        /// <summary>
        /// Output action that makes the agent to rotate clockwise
        /// </summary>
		private ExternalActionChunk outputRotateClockwise;
        /// <summary>
        /// Output action that makes the agent go ahead
        /// </summary>
		private ExternalActionChunk outputGoAhead;
        /// <summary>
        /// Output action that makes the agent eat some food
        /// </summary>
        private ExternalActionChunk outputEat;
        /// <summary>
        /// Output action that makes the agent sack something
        /// </summary>
        private ExternalActionChunk outputSack;
        /// <summary>
        /// Output action that makes the agent hide something
        /// </summary>
        private ExternalActionChunk outputHide;
        /// <summary>
        /// Output action that makes the agent stop
        /// </summary>
        private ExternalActionChunk outputStop;
        #endregion

        #endregion

        #region Constructor
        public ClarionAgent(WorldServer nws, String creature_ID, String creature_Name)
        {
			worldServer = nws;
			// Initialize the agent
            CurrentAgent = World.NewAgent("Current Agent");
			//mind = new Mind();
			//mind.Show();
			creatureId = creature_ID;
			creatureName = creature_Name;

            // Initialize Input Information
            inputWallAhead = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, DIMENSION_WALL_AHEAD);
            inputFoodAhead = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, DIMENSION_FOOD_AHEAD);
            inputLeafletJewelAhead = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, DIMENSION_LEAFLET_JEWEL_AHEAD);
            inputNonLeafletJewelAhead = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, DIMENSION_NON_LEAFLET_JEWEL_AHEAD);
            inputCloseObjectAhead = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, DIMENSION_CLOSE_OBJECT_AHEAD);
            inputHasCompletedLeaflet = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, DIMENSION_HAS_COMPLETED_LEAFLET);


            // Initialize Output actions
            outputRotateClockwise = World.NewExternalActionChunk(CreatureActions.ROTATE_CLOCKWISE.ToString());
            outputGoAhead = World.NewExternalActionChunk(CreatureActions.GO_AHEAD.ToString());
            outputEat = World.NewExternalActionChunk(CreatureActions.EAT.ToString());
            outputHide = World.NewExternalActionChunk(CreatureActions.HIDE.ToString());
            outputSack = World.NewExternalActionChunk(CreatureActions.SACK.ToString());
            outputStop = World.NewExternalActionChunk(CreatureActions.STOP.ToString());

            //Create thread to simulation
            runThread = new Thread(CognitiveCycle);
			Console.WriteLine("Agent started");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Run the Simulation in World Server 3d Environment
        /// </summary>
        public void Run()
        {                
			Console.WriteLine ("Running ...");
            // Setup Agent to run
            if (runThread != null && !runThread.IsAlive)
            {
                SetupAgentInfraStructure();
				// Start Simulation Thread                
                runThread.Start(null);
            }
        }

        /// <summary>
        /// Abort the current Simulation
        /// </summary>
        /// <param name="deleteAgent">If true beyond abort the current simulation it will die the agent.</param>
        public void Abort(Boolean deleteAgent)
        {   Console.WriteLine ("Aborting ...");
            if (runThread != null && runThread.IsAlive)
            {
                runThread.Abort();
            }

            if (CurrentAgent != null && deleteAgent)
            {
                CurrentAgent.Die();
            }
        }

		IList<Thing> processSensoryInformation()
		{
			IList<Thing> response = null;

			if (worldServer != null && worldServer.IsConnected)
			{
                response = worldServer.SendGetCreatureState(creatureName);

                if (response != null)
                {
                    prad = (Math.PI / 180) * response.First().Pitch;
                    while (prad > Math.PI)
                        prad -= 2 * Math.PI;
                    while (prad < -Math.PI)
                        prad += 2 * Math.PI;
                    Sack s = worldServer.SendGetSack("0");
                    //mind.setBag(s);
                }
            }

			return response;
		}

		void processSelectedAction(CreatureActions externalAction)
		{   Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			if (worldServer != null && worldServer.IsConnected)
			{
				switch (externalAction)
				{
				case CreatureActions.DO_NOTHING:
					// Do nothing as the own value says
					break;
				case CreatureActions.ROTATE_CLOCKWISE:
					worldServer.SendSetAngle(creatureId, 2, -2, 2);
					break;
                    case CreatureActions.GO_AHEAD:
                        if (targetThing != null)
                            worldServer.SendSetGoTo(creatureId, 1, 1, targetThing.comX, targetThing.comY);
                        break;
                    case CreatureActions.SACK:
						if (targetThing != null)
							worldServer.SendSackIt(creatureId, targetThing.Name);
                        break;
                    case CreatureActions.EAT:
                        if (targetThing != null)
                            worldServer.SendEatIt(creatureId, targetThing.Name);
                        break;
                    case CreatureActions.HIDE:
                        if (targetThing != null)
                            worldServer.SendHideIt(creatureId, targetThing.Name);
                        break;
                    case CreatureActions.STOP:
                        worldServer.SendStopCreature(creatureId);
                        break;
                    default:
					break;
				}
			}
		}

        #endregion

        #region Setup Agent Methods
        /// <summary>
        /// Setup agent infra structure (ACS, NACS, MS and MCS)
        /// </summary>
        private void SetupAgentInfraStructure()
        {
            // Setup the ACS Subsystem
            SetupACS();                    
        }

        private void SetupMS()
        {            
            //RichDrive
        }

        /// <summary>
        /// Setup the ACS subsystem
        /// </summary>
        private void SetupACS()
        {
            // Create Rule to rotate
            SupportCalculator rotateSupportCalculator = FixedRuleToRotate;
            FixedRule ruleRotate = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, outputRotateClockwise, rotateSupportCalculator);

            // Commit this rule to Agent (in the ACS)
            CurrentAgent.Commit(ruleRotate);

            // Create Colission To Go Ahead
            SupportCalculator goAheadSupportCalculator = FixedRuleToGoAhead;
            FixedRule ruleGoAhead = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, outputGoAhead, goAheadSupportCalculator);
            
            // Commit this rule to Agent (in the ACS)
            CurrentAgent.Commit(ruleGoAhead);

            // Create Colission To Eat
            SupportCalculator eatSupportCalculator = FixedRuleToEat;
            FixedRule ruleEat = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, outputEat, eatSupportCalculator);

            // Commit this rule to Agent (in the ACS)
            CurrentAgent.Commit(ruleEat);

            // Create Colission To Sack
            SupportCalculator sackSupportCalculator = FixedRuleToSack;
            FixedRule ruleSack = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, outputSack, sackSupportCalculator);

            // Commit this rule to Agent (in the ACS)
            CurrentAgent.Commit(ruleSack);

            // Create Colission To Hide
            SupportCalculator hideSupportCalculator = FixedRuleToHide;
            FixedRule ruleHide = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, outputHide, hideSupportCalculator);

            // Commit this rule to Agent (in the ACS)
            CurrentAgent.Commit(ruleHide);

            // Create Colission To Stop
            SupportCalculator stopSupportCalculator = FixedRuleToStop;
            FixedRule ruleStop = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, outputStop, stopSupportCalculator);

            // Commit this rule to Agent (in the ACS)
            CurrentAgent.Commit(ruleStop);

            // Disable Rule Refinement
            CurrentAgent.ACS.Parameters.PERFORM_RER_REFINEMENT = false;

            // The selection type will be probabilistic
            CurrentAgent.ACS.Parameters.LEVEL_SELECTION_METHOD = ActionCenteredSubsystem.LevelSelectionMethods.STOCHASTIC;

            // The action selection will be fixed (not variable) i.e. only the statement defined above.
            CurrentAgent.ACS.Parameters.LEVEL_SELECTION_OPTION = ActionCenteredSubsystem.LevelSelectionOptions.FIXED;

            // Define Probabilistic values
            CurrentAgent.ACS.Parameters.FIXED_FR_LEVEL_SELECTION_MEASURE = 1;
            CurrentAgent.ACS.Parameters.FIXED_IRL_LEVEL_SELECTION_MEASURE = 0;
            CurrentAgent.ACS.Parameters.FIXED_BL_LEVEL_SELECTION_MEASURE = 0;
            CurrentAgent.ACS.Parameters.FIXED_RER_LEVEL_SELECTION_MEASURE = 0;
        }

        /// <summary>
        /// Make the agent perception. In other words, translate the information that came from sensors to a new type that the agent can understand
        /// </summary>
        /// <param name="sensorialInformation">The information that came from server</param>
        /// <returns>The perceived information</returns>
		private SensoryInformation prepareSensoryInformation(IList<Thing> listOfThings)
        {
            // New sensory information
            SensoryInformation si = World.NewSensoryInformation(CurrentAgent);

            Creature c = (Creature) listOfThings.Where(item => (item.CategoryId == Thing.CATEGORY_CREATURE)).First(); 
            targetThing = listOfThings.Where(item => (item.CategoryId != Thing.CATEGORY_CREATURE && item.CategoryId != Thing.CATEGORY_BRICK)).OrderBy(x => x.DistanceToCreature).FirstOrDefault();
       
            List<Leaflet> leaflets = c.getLeaflets();

            // Detect if we have a wall ahead
            //Boolean wallAhead = listOfThings.Where(item => (item.CategoryId == Thing.CATEGORY_BRICK && item.DistanceToCreature <= 61)).Any();
            //double wallAheadActivationValue = wallAhead ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;

            Boolean foodAhead = targetThing == null ? false : targetThing.CategoryId == Thing.categoryPFOOD || targetThing.CategoryId == Thing.CATEGORY_NPFOOD;
            double foodAheadActivationValue = foodAhead ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;

            Boolean leafletJewelAhead = targetThing == null ? false : targetThing.CategoryId == Thing.CATEGORY_JEWEL && hasToGetJewelForLeaflet(targetThing.Material.Color, leaflets.FirstOrDefault());
            double leafletJewelAheadActivationValue = leafletJewelAhead ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;

            Boolean nonLeafletJewelAhead = targetThing == null ? false : leafletJewelAhead == false && targetThing.CategoryId == Thing.CATEGORY_JEWEL;
            double nonLeafletJewelAheadActivationValue = nonLeafletJewelAhead ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;

            Boolean closeObjectAhead = targetThing == null ? false : targetThing.DistanceToCreature < 40;
            double closeObjectAheadActivationValue = closeObjectAhead ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;

            Boolean hasCompletedLeaflet = leaflets[0].situation;
            double hasCompletedLeafletActivationValue = hasCompletedLeaflet ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;

            si.Add(inputFoodAhead, foodAheadActivationValue);
            si.Add(inputLeafletJewelAhead, leafletJewelAheadActivationValue);
            si.Add(inputNonLeafletJewelAhead, nonLeafletJewelAheadActivationValue);
            si.Add(inputCloseObjectAhead, closeObjectAheadActivationValue);
            si.Add(inputHasCompletedLeaflet, hasCompletedLeafletActivationValue);
     
            
            //Console.WriteLine(sensorialInformation);
            int n = 0;
			foreach(Leaflet l in c.getLeaflets()) {
				//mind.updateLeaflet(n,l);
				n++;
			}
			//mind.update();
			leaflets.First().PrintLeaflet(0);
            return si;
        }
        #endregion

        #region Fixed Rules
        private double FixedRuleToRotate(ActivationCollection currentInput, Rule target)
        {
            // See partial match threshold to verify what are the rules available for action selection
            return ((currentInput.Contains(inputCloseObjectAhead, CurrentAgent.Parameters.MIN_ACTIVATION)) &&
                    (currentInput.Contains(inputFoodAhead, CurrentAgent.Parameters.MIN_ACTIVATION)) &&
                    (currentInput.Contains(inputLeafletJewelAhead, CurrentAgent.Parameters.MIN_ACTIVATION)) &&
                    (currentInput.Contains(inputNonLeafletJewelAhead, CurrentAgent.Parameters.MIN_ACTIVATION))) ? 1.0 : 0.0;
        }

        private double FixedRuleToGoAhead(ActivationCollection currentInput, Rule target)
        {
            // Here we will make the logic to go ahead
            return ((currentInput.Contains(inputFoodAhead, CurrentAgent.Parameters.MAX_ACTIVATION)) ||
                    (currentInput.Contains(inputLeafletJewelAhead, CurrentAgent.Parameters.MAX_ACTIVATION)) ||
                    (currentInput.Contains(inputNonLeafletJewelAhead, CurrentAgent.Parameters.MAX_ACTIVATION)) &&
                    (currentInput.Contains(inputCloseObjectAhead, CurrentAgent.Parameters.MIN_ACTIVATION))) ? 1.0 : 0.0;
        }

        private double FixedRuleToEat(ActivationCollection currentInput, Rule target)
        {
            // Here we will make the logic to go ahead
            return ((currentInput.Contains(inputFoodAhead, CurrentAgent.Parameters.MAX_ACTIVATION)) &&
                    (currentInput.Contains(inputCloseObjectAhead, CurrentAgent.Parameters.MAX_ACTIVATION))) ? 1.0 : 0.0;
        }

        private double FixedRuleToSack(ActivationCollection currentInput, Rule target)
        {
            // Here we will make the logic to go ahead
            return ((currentInput.Contains(inputLeafletJewelAhead, CurrentAgent.Parameters.MAX_ACTIVATION)) &&
                    (currentInput.Contains(inputCloseObjectAhead, CurrentAgent.Parameters.MAX_ACTIVATION))) ? 1.0 : 0.0;
        }

        private double FixedRuleToHide(ActivationCollection currentInput, Rule target)
        {
            // Here we will make the logic to go ahead
            return ((currentInput.Contains(inputNonLeafletJewelAhead, CurrentAgent.Parameters.MAX_ACTIVATION)) &&
                    (currentInput.Contains(inputCloseObjectAhead, CurrentAgent.Parameters.MAX_ACTIVATION))) ? 1.0 : 0.0;
        }

        private double FixedRuleToStop(ActivationCollection currentInput, Rule target)
        {
            // Here we will make the logic to go ahead
            return ((currentInput.Contains(inputHasCompletedLeaflet, CurrentAgent.Parameters.MAX_ACTIVATION))) ? 1.0 : 0.0;
        }

        #endregion

        #region Run Thread Method
        private void CognitiveCycle(object obj)
        {

			Console.WriteLine("Starting Cognitive Cycle ... press CTRL-C to finish !");
            // Cognitive Cycle starts here getting sensorial information
            while (CurrentCognitiveCycle != MaxNumberOfCognitiveCycles)
            {   
				// Get current sensory information                    
				IList<Thing> currentSceneInWS3D = processSensoryInformation();

                if (currentSceneInWS3D != null) {
					// Make the perception
					SensoryInformation si = prepareSensoryInformation (currentSceneInWS3D);

					//Perceive the sensory information
					CurrentAgent.Perceive (si);

					//Choose an action
					ExternalActionChunk chosen = CurrentAgent.GetChosenExternalAction (si);

					// Get the selected action
					String actionLabel = chosen.LabelAsIComparable.ToString ();
					CreatureActions actionType = (CreatureActions)Enum.Parse (typeof(CreatureActions), actionLabel, true);

					// Call the output event handler
					processSelectedAction (actionType);

					// Increment the number of cognitive cycles
					CurrentCognitiveCycle++;

					//Wait to the agent accomplish his job
					if (TimeBetweenCognitiveCycles > 0) {
						Thread.Sleep (TimeBetweenCognitiveCycles);
					}
				}
			}
        }
        #endregion

        private Boolean hasToGetJewelForLeaflet(String color, Leaflet leaflet)
        {
            return leaflet.getRequired(color) > leaflet.getCollected(color);
        }

        private Boolean hasToGetJewelForLeaflets(String color, List<Leaflet> leaflets)
        {
            return leaflets.Where(leaflet => leaflet.getRequired(color) > leaflet.getCollected(color)).Any();
        }
    }
}
