using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using MachinationsUP.GameEngineAPI.Game;
using MachinationsUP.GameEngineAPI.GameObject;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.SyncAPI;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.GameObject;
using MachinationsUP.Integration.Inventory;
using SocketIO;
using UnityEngine;

namespace MachinationsUP.Engines.Unity
{
    /// <summary>
    /// The Machinations Game Layer is a game-wide Singleton that handles communication with the Machinations back-end.
    /// </summary>
    public class MachinationsGameLayer : MonoBehaviour, IGameLifecycleSubscriber, IGameObjectLifecycleSubscriber
    {

        #region Variables

        #region Editor-Defined

        /// <summary>
        /// The User Key under which to make all API calls. This can be retrieved from
        /// the Machinations product.
        /// </summary>
        public string userKey;

        /// <summary>
        /// Game name is be used for associating a game with multiple diagrams.
        /// [TENTATIVE UPCOMING SUPPORT]
        /// </summary>
        public string gameName;

        /// <summary>
        /// The Machinations Diagram Token will be used to identify ONE Diagram that this game is connected to.
        /// </summary>
        public string diagramToken;

        /// <summary>
        /// Name of the directory where to store the Cache. If defined, then the MGL will store all values received from
        /// Machinations within this directory inside an XML file. Upon startup, if the connection to the Machinations Back-end
        /// is not operational, the Cache will be used. This system can also be used to provide versioning between different
        /// snapshots of data received from Machinations.
        /// </summary>
        public string cacheDirectoryName;

        #endregion

        #region Public

        private IGameLifecycleProvider _gameLifecycleProvider;

        /// <summary>
        /// Used by MachinationsGameAwareObjects to query Game State.
        /// </summary>
        public IGameLifecycleProvider GameLifecycleProvider
        {
            set => _gameLifecycleProvider = value;
        }

        /// <summary>
        /// Global Event Handler for any incoming update from Machinations back-end.
        /// </summary>
        static public EventHandler OnMachinationsUpdate;

        /// <summary>
        /// Will throw exceptions if values are not found in Offline mode.
        /// </summary>
        static public bool StrictOfflineMode = false;

        #endregion

        #region Private

        /// <summary>
        /// This Dictionary contains ALL Machinations Diagram Elements that can possibly be retrieved
        /// during the lifetime of the game. This is generated based on ALL the
        /// <see cref="MachinationsUP.Integration.Inventory.MachinationsGameObjectManifest"/> declared in the game.
        ///
        /// New MachinationElements are created from the ones in this Dictionary.
        ///
        /// Dictionary of the <see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/> indicating where in the Diagram to find a
        /// Game Object Property Name and the <see cref="ElementBase"/> that will serve as a Source value
        /// to all values that will be created by the MachinationsGameLayer.
        /// </summary>
        static readonly private Dictionary<DiagramMapping, ElementBase> _sourceElements =
            new Dictionary<DiagramMapping, ElementBase>();

        /// <summary>
        /// Disk XML cache.
        /// See <see cref="MachinationsUP.Integration.Inventory.MCache"/>.
        /// </summary>
        static private MCache _cache = new MCache();

        /// <summary>
        /// List with of all registered MachinationsGameObject.
        /// </summary>
        static readonly private List<MachinationsGameObject> _gameObjects = new List<MachinationsGameObject>();

        /// <summary>
        /// List with of all registered MachinationsGameAwareObject.
        /// </summary>
        static readonly private List<MachinationsGameAwareObject> _gameAwareObjects = new List<MachinationsGameAwareObject>();

        /// <summary>
        /// Dictionary with Scriptable Objects and their associated Binders (per Game Object Property name).
        /// </summary>
        static readonly private Dictionary<IMachinationsScriptableObject, EnrolledScriptableObject> _scriptableObjects =
            new Dictionary<IMachinationsScriptableObject, EnrolledScriptableObject>();

        /// <summary>
        /// Socket.io used for communication with Machinations NodeJS backend.
        /// </summary>
        private SocketIOComponent _socket;

        /// <summary>
        /// Number of responses that are pending from the Socket.
        /// </summary>
        private int _pendingResponses;

        /// <summary>
        /// Connection to Machinations Backend has been aborted.
        /// </summary>
        static private bool _connectionAborted;

        #endregion

        #endregion

        #region Implementation of IGameLifecycleSubscriber

        /// <summary>
        /// Returns the current Game State.
        /// </summary>
        public GameStates CurrentGameState { get; private set; }

        /// <summary>
        /// Informs an IGameLifecycleSubscriber about a new Game State.
        /// </summary>
        /// <param name="newGameState">New Game State.</param>
        public void OnGameStateChanged (GameStates newGameState)
        {
            if (newGameState == CurrentGameState) return;
            foreach (MachinationsGameAwareObject mgao in _gameAwareObjects)
                mgao.OnGameStateChanged(newGameState);
            CurrentGameState = newGameState;
        }

        /// <summary>
        /// Informs an IGameLifecycleSubscriber that a Game Event occured.
        /// </summary>
        /// <param name="evnt"></param>
        public void OnGameEvent (string evnt)
        {
        }

        /// <summary>
        /// Machinations -> Game commands. Intended for Future use.
        /// </summary>
        /// <param name="command"></param>
        public void GameCommand (MachinationsCommands command)
        {
        }

        #endregion

        #region Implementation of IGameObjectLifecycleSubscriber

        /// <summary>
        /// Returns the current Game Object State.
        /// </summary>
        public GameObjectStates CurrentGameObjectState { get; private set; }

        /// <summary>
        /// Informs an IGameObjectLifecycleSubscriber about a new Game Object State.
        /// </summary>
        /// <param name="newGameObjectState">New Game Object State.</param>
        public void OnGameObjectStateChanged (GameObjectStates newGameObjectState)
        {
            if (newGameObjectState == CurrentGameObjectState) return;
            foreach (MachinationsGameAwareObject mgao in _gameAwareObjects)
                mgao.OnGameObjectStateChanged(newGameObjectState);
            CurrentGameObjectState = newGameObjectState;
        }

        /// <summary>
        /// Informs an IGameObjectLifecycleSubscriber that a Game Object Event occured.
        /// </summary>
        /// <param name="evnt"></param>
        public void OnGameObjectEvent (string evnt)
        {
            throw new NotSupportedException("Not supported. This is only for MachinationsGameAwareObjects.");
        }

        #endregion

        #region Singleton

        /// <summary>
        /// Singleton instance.
        /// </summary>
        static private MachinationsGameLayer _instance;

        /// <summary>
        /// Singleton implementation.
        /// </summary>
        static public MachinationsGameLayer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new GameObject("MachinationsGameLayer").AddComponent<MachinationsGameLayer>();
                Debug.Log("MGL created by invocation. Hash is " + _instance.GetHashCode() + " and User Key is " + _instance.userKey);
                return _instance;
            }
        }

        #endregion

        #region MonoBehaviour Overrides (Entry Point is in Start)

        /// <summary>
        /// Awake is used to initialize any variables or game state before the game starts.
        /// Awake is called only once during the lifetime of the script instance.
        /// Awake is called after all objects are initialized so you can safely speak to other objects
        /// or query them using for example GameObject.FindWithTag.
        /// </summary>
        private void Awake ()
        {
            if (_instance == null)
            {
                //If the MGL is added to the Scene as a prefab (as it should), then this function
                //will likely execute before Instance is ever accessed. Making sure that the instance is set.
                Debug.Log("MGL created by Unity Engine. Hash is " + GetHashCode() + " and User Key is " + userKey);
                _instance = this;
            }

            Debug.Log("MGL Awake.");
            Debug.Log(gameObject); //Show the object to which this script is attached, if any.
        }

        /// <summary>
        /// Start is called before the first frame update.
        /// </summary>
        private IEnumerator Start ()
        {
            Debug.Log("MGL.Start: Pausing game during initialization.");

            foreach (MachinationsGameObject mgo in _gameObjects)
                AddTargets(mgo.Manifest.GetMachinationsDiagramTargets());

            //Notify Game Engine of Machinations Init Start.
            Instance._gameLifecycleProvider.MachinationsInitStart();

            //Attempt to init Socket.
            _connectionAborted = InitSocket() == false;

            yield return new WaitUntil(() => _connectionAborted || _socket.IsConnected);

            if (_connectionAborted)
            {
                Debug.LogError("MGL Connection failure. Game will proceed with default/cached values!");

                //Cache system active? Load Cache.
                if (!string.IsNullOrEmpty(cacheDirectoryName)) LoadCache();
                //Running in offline mode now.
                IsInOfflineMode = true;
                OnMachinationsUpdate?.Invoke(this, null);
            }
            else
            {
                Debug.Log("MGL.Start: Connection achieved. Authentication request.");

                //Authenticate to Machinations back-end.
                EmitMachinationsAuthRequest();

                yield return new WaitUntil(() => IsAuthenticated || IsInOfflineMode);

                Debug.Log("MGL.Start: Authenticated. Sync start.");

                //Send Game Init Requests for all registered machinationsUniqueID.
                EmitMachinationsInitRequest();

                yield return new WaitUntil(() => IsInitialized || IsInOfflineMode);

                Debug.Log("MGL.Start: Machinations Backend Sync complete. Resuming game.");
            }

            //Notify Game Engine of Machinations Init Complete.
            Instance._gameLifecycleProvider.MachinationsInitComplete();
        }

        #endregion

        #region Internal Functionality

        /// <summary>
        /// Initializes the Socket IO component.
        /// </summary>
        private bool InitSocket ()
        {
            //Initialize socket.
            GameObject go = GameObject.Find("SocketIO");
            //No Socket found.
            if (go == null) return false;
            Debug.Log("MGL.Start: Initiating connection to Machinations Backend.");
            //TODO: during development, fail fast on Socket error. Remove @ release.
            SocketIOComponent.MaxRetryCountForConnect = 1;
            _socket = go.GetComponent<SocketIOComponent>();
            _socket.SetUserKey(userKey);
            _socket.Init();
            _socket.On("open", OnSocketOpen);
            _socket.On(SyncMsgs.RECEIVE_AUTH_SUCCESS, OnAuthSuccess);
            _socket.On(SyncMsgs.RECEIVE_AUTH_DENY, OnAuthDeny);
            _socket.On(SyncMsgs.RECEIVE_GAME_INIT, OnGameInitResponse);
            _socket.On(SyncMsgs.RECEIVE_DIAGRAM_ELEMENTS_UPDATED, OnDiagramElementsUpdated);
            _socket.On("error", OnSocketError);
            _socket.On("close", OnSocketClose);
            _socket.Connect();
            return true;
        }

        /// <summary>
        /// Notifies all enrolled <see cref="MachinationsUP.Integration.GameObject.MachinationsGameObject"/> that
        /// the MGL is now initialized.
        /// </summary>
        /// <param name="isRunningOffline">TRUE: the MGL is running in offline mode.</param>
        static private void NotifyAboutMGLInitComplete (bool isRunningOffline = false)
        {
            Debug.Log("MGL NotifyAboutMGLInitComplete.");
            //Notify Scriptable Objects.
            foreach (IMachinationsScriptableObject so in _scriptableObjects.Keys)
            {
                _scriptableObjects[so].Binders = CreateBindersForManifest(_scriptableObjects[so].Manifest);
                so.MGLInitCompleteSO(_scriptableObjects[so].Binders);
            }

            //_gameObjects is cloned as a new Array because the collection MAY be modified during MachinationsGameObject.MGLInitComplete.
            //That's because Game Objects MAY create other Game Objects during MachinationsGameObject.MGLInitComplete.
            //These new Game Objects will then enroll with the MGL, which will add them to _gameObjects.
            List<MachinationsGameObject> gameObjectsToNotify = new List<MachinationsGameObject>(_gameObjects.ToArray());
            //Maintain a list of Game Objects that were notified.
            List<MachinationsGameObject> gameObjectsNotified = new List<MachinationsGameObject>();
            //Maintain a list of all Game Objects that were created during the notification loop.
            List<MachinationsGameObject> gameObjectsCreatedDuringNotificationLoop = new List<MachinationsGameObject>();

            do
            {
                Debug.Log("MGL NotifyAboutMGLInitComplete gameObjectsToNotify: " + gameObjectsToNotify.Count);

                //Notify Machinations Game Objects.
                foreach (MachinationsGameObject mgo in gameObjectsToNotify)
                {
                    //Debug.Log("MGL NotifyAboutMGLInitComplete: Notifying: " + mgo);

                    //This may create new Machinations Game Objects due to them subscribing to MachinationsGameObject.OnBindersUpdated which
                    //is called on MGLInitComplete. Game Objects may create other Game Objects at that point.
                    //For an example: See Unity Example Ruby's Adventure @ EnemySpawner.OnBindersUpdated [rev f99963842e9666db3e697da5446e47cb5f1c4225]
                    mgo.MGLInitComplete(isRunningOffline);
                    gameObjectsNotified.Add(mgo);
                }

                //Debug.Log("MGL NotifyAboutMGLInitComplete gameObjectsNotified: " + gameObjectsNotified.Count);

                //Clearing our task list of objects to notify.
                gameObjectsToNotify.Clear();

                //Check if any new Game Objects were created & enrolled during the above notification loop.
                foreach (MachinationsGameObject mgo in _gameObjects)
                    if (!gameObjectsNotified.Contains(mgo))
                    {
                        //DEBT: [working on MGO lifecycle] we've commented out adding new Game Objects to gameObjectsToNotify because
                        //we want to only trigger MGLInitComplete on items that were already created. If they create other items,
                        //they will instead receive MGLReady upon Enrolling.
                        //gameObjectsToNotify.Add(mgo);

                        //Keep track of how many new objects we created during the notification loop(s).
                        gameObjectsCreatedDuringNotificationLoop.Add(mgo);
                    }

                //Debug.Log("MGL NotifyAboutMGLInitComplete NEW gameObjectsToNotify: " + gameObjectsToNotify.Count);
            }
            //New objects were created.
            while (gameObjectsToNotify.Count > 0);

            Debug.Log("MGL NotifyAboutMGLInitComplete gameObjectsCreatedDuringNotificationLoop: " +
                      gameObjectsCreatedDuringNotificationLoop.Count);
        }

        /// <summary>
        /// Concatenates a Dictionary of Unique Machination IDs and their associated ElementBase to
        /// the Game Layer's repository. All of the ElementBase in the repository will be initialized upon
        /// game startup.
        /// </summary>
        /// <param name="targets"></param>
        static private void AddTargets (Dictionary<DiagramMapping, ElementBase> targets)
        {
            foreach (DiagramMapping diagramMapping in targets.Keys)
            {
                //Only add new targets.
                if (_sourceElements.ContainsKey(diagramMapping)) continue;
                _sourceElements.Add(diagramMapping, targets[diagramMapping]);
            }
        }

        /// <summary>
        /// Creates <see cref="ElementBinder"/> for each Game Object Property provided in the <see cref="MachinationsGameObjectManifest"/>.
        /// </summary>
        /// <returns>Dictionary of Game Object Property Name and ElementBinder.</returns>
        static private Dictionary<string, ElementBinder> CreateBindersForManifest (MachinationsGameObjectManifest manifest)
        {
            var ret = new Dictionary<string, ElementBinder>();
            foreach (DiagramMapping dm in manifest.PropertiesToSync)
            {
                ElementBinder eb = new ElementBinder(null, dm); //The Binder will NOT have any Parent Game Object.
                //Ask the Binder to create Elements for all possible States Associations.
                var statesAssociations = manifest.GetStatesAssociationsForPropertyName(dm.GameObjectPropertyName);
                //If no States Associations were defined.
                if (statesAssociations.Count == 0)
                    eb.CreateElementBaseForStateAssoc();
                else
                    foreach (StatesAssociation statesAssociation in statesAssociations)
                        eb.CreateElementBaseForStateAssoc(statesAssociation);
                //Save the Binder for later use.
                dm.Binder = eb;
                //Store the new Binder in the Dictionary to return.
                ret[dm.GameObjectPropertyName] = eb;
            }

            return ret;
        }

        /// <summary>
        /// Returns a string that can be later decomposed in order to find an element in a Machinations Diagram.
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="statesAssociation"></param>
        /// <returns></returns>
        virtual protected string GetMachinationsUniqueID (ElementBinder binder, StatesAssociation statesAssociation)
        {
            return (binder.ParentGameObject != null ? binder.ParentGameObject.GameObjectName : "!NoParent!") + "." +
                   binder.GameObjectPropertyName + "." +
                   (statesAssociation != null ? statesAssociation.Title : "N/A");
        }

        /// <summary>
        /// Finds an ElementBase within the Diagram Mappings.
        /// </summary>
        /// <param name="elementBinder">The ElementBinder that should match the ElementBase.</param>
        /// <param name="statesAssociation">The StatesAssociation to search with.</param>
        public ElementBase FindSourceElement (ElementBinder elementBinder,
            StatesAssociation statesAssociation = null)
        {
            ElementBase ret = null;
            bool found = false;
            //Search all Diagram Mappings to see which one matches the provided Binder and States Association.
            foreach (DiagramMapping diagramMapping in _sourceElements.Keys)
                if (diagramMapping.Matches(elementBinder, statesAssociation, HasCache))
                {
                    ret = _sourceElements[diagramMapping];
                    found = true;
                    break;
                }

            //A DiagramMapping must have been found for this element.
            if (!found)
                throw new Exception("MGL.FindSourceElement: machinationsUniqueID '" +
                                    GetMachinationsUniqueID(elementBinder, statesAssociation) +
                                    "' not found in _sourceElements.");
            //If no ElementBase was found.
            if (found && ret == null)
            {
                //Search the Cache.
                if (IsInOfflineMode && HasCache)
                    foreach (DiagramMapping diagramMapping in _cache.DiagramMappings)
                        if (diagramMapping.Matches(elementBinder, statesAssociation, true))
                            return diagramMapping.CachedElementBase;

                //Nothing found? Throw!
                if (!isInOfflineMode || (IsInOfflineMode && StrictOfflineMode))
                    throw new Exception("MGL.FindSourceElement: machinationsUniqueID '" +
                                        GetMachinationsUniqueID(elementBinder, statesAssociation) +
                                        "' has not been initialized.");
            }

            return ret;
        }

        /// <summary>
        /// Updates the <see cref="_sourceElements"/> with values from the Machinations Back-end. Only initializes those values
        /// that have been registered via <see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/>. If entirely new
        /// values come from the back-end, will throw an Exception.
        /// </summary>
        /// <param name="elementsFromBackEnd">List of <see cref="JSONObject"/> received from the Socket IO Component.</param>
        /// <param name="updateFromDiagram">TRUE: update existing elements. If FALSE, will throw Exceptions on collisions.</param>
        private void UpdateWithValuesFromMachinations (List<JSONObject> elementsFromBackEnd, bool updateFromDiagram = false)
        {
            //The response is an Array of key-value pairs PER Machination Diagram ID.
            //Each of these maps to a certain member of _sourceElements.
            foreach (JSONObject diagramElement in elementsFromBackEnd)
            {
                //Dictionary of SyncMsgs.JP_DIAGRAM_* and their value.
                var elementProperties = new Dictionary<string, string>();
                int i = 0;
                //Get all properties in a Dictionary, since their order is not guaranteed in a list.
                foreach (string machinationsPropertyName in diagramElement.keys)
                    elementProperties.Add(machinationsPropertyName, diagramElement[i++].ToString().Replace("\"", ""));

                //Find Diagram Mapping matching the provided Machinations Diagram ID.
                DiagramMapping diagramMapping = GetDiagramMappingForID(elementProperties["id"]);

                //Get the Element Base based on the dictionary of Element Properties.
                ElementBase elementBase = CreateElementFromProps(elementProperties);
                Debug.Log("ElementBase created for '" + diagramMapping + "' with Base Value of: " +
                          elementBase.BaseValue);

                //Element already exists but not in Update mode?
                if (_sourceElements[diagramMapping] != null && !updateFromDiagram)
                    throw new Exception("MGL.UpdateWithValuesFromMachinations: A Source Element already exists for this DiagramMapping: " +
                                        diagramMapping +
                                        ". Perhaps you wanted to update it? Then, invoke this function with update: true.");

                //This is the important line where the ElementBase is assigned to the Source Elements Dictionary, to be used for
                //cloning elements in the future.
                _sourceElements[diagramMapping] = elementBase;

                //Caching active? Update the Cache.
                if (!string.IsNullOrEmpty(cacheDirectoryName))
                {
                    diagramMapping.CachedElementBase = elementBase;
                    if (!_cache.DiagramMappings.Contains(diagramMapping)) _cache.DiagramMappings.Add(diagramMapping);
                }

                //When changes occur in the Diagram, the Machinations back-end will notify UP.
                if (updateFromDiagram) NotifyAboutMGLUpdate(diagramMapping, elementBase);
            }

            //Send Update notification to all listeners.
            OnMachinationsUpdate?.Invoke(this, null);
            //Caching active? Save the cache now.
            if (!string.IsNullOrEmpty(cacheDirectoryName)) SaveCache();
        }

        /// <summary>
        /// Goes through registered <see cref="MachinationsGameObject"/> and <see cref="IMachinationsScriptableObject"/>
        /// and notifies those Objects that are affected by the update.
        /// </summary>
        /// <param name="diagramMapping">Diagram Mapping that has been updated.</param>
        /// <param name="elementBase">The <see cref="ElementBase"/> parsed from the back-end update.</param>
        private void NotifyAboutMGLUpdate (DiagramMapping diagramMapping, ElementBase elementBase)
        {
            //Notify Scriptable Objects that are affected by what changed in this update.
            foreach (IMachinationsScriptableObject imso in _scriptableObjects.Keys)
            {
                //Find matching Binder by checking Game Object Property names.
                //Reminder: _scriptableObjects[imso] is a Dictionary of that Machinations Scriptable Object's Game Property Names.
                foreach (string gameObjectPropertyName in _scriptableObjects[imso].Binders.Keys)
                    if (gameObjectPropertyName == diagramMapping.GameObjectPropertyName)
                    {
                        //TODO: must update instead of create here.
                        _scriptableObjects[imso].Binders[gameObjectPropertyName]
                            .CreateElementBaseForStateAssoc(diagramMapping.StatesAssoc, true);
                        imso.MGLUpdateSO(diagramMapping, elementBase);
                    }
            }

            //Notify all registered Machinations Game Objects, if they have 
            foreach (MachinationsGameObject mgo in _gameObjects)
                //When we find a registered Game Object that matches this Diagram Mapping asking it to update its Binder.
                if (mgo.GameObjectName == diagramMapping.GameObjectName)
                    mgo.UpdateBinder(diagramMapping, elementBase);
        }

        /// <summary>
        /// Retrieves the <see cref="DiagramMapping"/> for the requested Machinations Diagram ID.
        /// </summary>
        static private DiagramMapping GetDiagramMappingForID (string machinationsDiagramID)
        {
            DiagramMapping diagramMapping = null;
            foreach (DiagramMapping dm in _sourceElements.Keys)
                if (dm.DiagramElementID == int.Parse(machinationsDiagramID))
                {
                    return dm;
                }

            //Couldn't find any Binding for this Machinations Diagram ID.
            if (diagramMapping == null)
                throw new Exception("MGL.UpdateWithValuesFromMachinations: Got from the back-end a Machinations Diagram ID (" +
                                    machinationsDiagramID + ") for which there is no DiagramMapping.");
            return null;
        }

        /// <summary>
        /// Creates an <see cref="ElementBase"/> based on the provided properties from the Machinations Back-end.
        /// </summary>
        /// <param name="elementProperties">Dictionary of Machinations-specific properties.</param>
        private ElementBase CreateElementFromProps (Dictionary<string, string> elementProperties)
        {
            ElementBase elementBase;
            //Populate value inside Machinations Element.
            int iValue;
            string sValue;
            try
            {
                iValue = int.Parse(elementProperties["resources"]);
                elementBase = new ElementBase(iValue);
                //Set MaxValue, if we have from.
                if (elementProperties.ContainsKey("capacity") && int.TryParse(elementProperties["capacity"],
                    out iValue) && iValue != -1 && iValue != 0)
                {
                    elementBase.MaxValue = iValue;
                }
            }
            catch
            {
                sValue = elementProperties["label"];
                elementBase = new FormulaElement(sValue, false);
            }

            return elementBase;
        }
        
        /// <summary>
        /// Called on Socket Errors.
        /// </summary>
        private void FailedToConnect ()
        {
            if (_pendingResponses > 0)  _pendingResponses--;
            _connectionAborted = true;
            _socket.autoConnect = false;
            
            //Cache system active? Load Cache.
            if (!string.IsNullOrEmpty(cacheDirectoryName)) LoadCache();
            //Running in offline mode now.
            IsInOfflineMode = true;
            OnMachinationsUpdate?.Invoke(this, null);
        }

        /// <summary>
        /// Saves the Cache.
        /// </summary>
        private void SaveCache ()
        {
            string cachePath = Path.Combine(Application.dataPath, "MachinationsCache", cacheDirectoryName);
            string cacheFilePath = Path.Combine(cachePath, "Cache.xml");
            Directory.CreateDirectory(cachePath);
            Debug.Log("MGL.SaveCache using file: " + cacheFilePath);

            DataContractSerializer dcs = new DataContractSerializer(typeof(MCache));
            var settings = new XmlWriterSettings {Indent = true, NewLineOnAttributes = true};
            XmlWriter xmlWriter = XmlWriter.Create(cacheFilePath, settings);
            dcs.WriteObject(xmlWriter, _cache);
            xmlWriter.Close();
        }

        /// <summary>
        /// Loads the Cache located at <see cref="cacheDirectoryName"/>. Applies it over <see cref="_sourceElements"/>.
        /// </summary>
        private void LoadCache ()
        {
            string cacheFilePath = Path.Combine(Application.dataPath, "MachinationsCache", cacheDirectoryName, "Cache.xml");
            if (!File.Exists(cacheFilePath))
            {
                Debug.Log("MGL.LoadCache DOES NOT EXIST: " + cacheFilePath);
                _cache = null;
                return;
            }

            Debug.Log("MGL.LoadCache using file: " + cacheFilePath);

            //Deserialize Cache.
            DataContractSerializer dcs = new DataContractSerializer(typeof(MCache));
            FileStream fs = new FileStream(cacheFilePath, FileMode.Open);
            _cache = (MCache) dcs.ReadObject(fs);

            //Applying Cache.
            foreach (DiagramMapping dm in _cache.DiagramMappings)
            {
                //Cloning list elements because we'll be tampering with the collection.
                List<DiagramMapping> sourceKeys = new List<DiagramMapping>(_sourceElements.Keys.ToArray());
                for (int i = 0; i < sourceKeys.Count; i++)
                {
                    DiagramMapping dms = sourceKeys[i];
                    if (dms.Matches(dm, true))
                        _sourceElements[dms] = dm.CachedElementBase;
                }
            }
        }

        #endregion

        #region Socket IO - Communication with Machinations Back-end

        /// <summary>
        /// Handles event emission for a MachinationsGameObject.
        /// </summary>
        /// <param name="mgo">MachinationsGameObject that emitted the event.</param>
        /// <param name="evnt">The event that was emitted.</param>
        public void EmitEvent (MachinationsGameObject mgo, string evnt)
        {
            var sync = new Dictionary<string, string>
            {
                {SyncMsgs.JK_EVENT_GAME_OBJ_NAME, mgo.GameObjectName},
                {SyncMsgs.JK_EVENT_GAME_EVENT, evnt}
            };
            _socket.Emit(SyncMsgs.SEND_GAME_EVENT, new JSONObject(sync));
        }

        /// <summary>
        /// Emits the 'Game Auth Request' Socket event.
        /// </summary>
        private void EmitMachinationsAuthRequest ()
        {
            _pendingResponses++;
            var initRequest = new Dictionary<string, string>
            {
                {SyncMsgs.JK_AUTH_GAME_NAME, gameName},
                {SyncMsgs.JK_AUTH_DIAGRAM_TOKEN, diagramToken}
            };
            _socket.Emit(SyncMsgs.SEND_API_AUTHORIZE, new JSONObject(initRequest));
        }

        /// <summary>
        /// Emits the 'Game Init Request' Socket event.
        /// </summary>
        private void EmitMachinationsInitRequest ()
        {
            //Make sure we're stopping the game until the answer comes back.
            _pendingResponses++;

            //Init Request components will be stored as top level items in this Dictionary.
            var initRequest = new Dictionary<string, JSONObject>();

            //Create individual JSON Objects for each Machination element to retrieve.
            //This is an Array because this is what the JSON Object Library expects.
            JSONObject[] keys = new JSONObject [_sourceElements.Keys.Count];
            int i = 0;
            foreach (DiagramMapping diagramMapping in _sourceElements.Keys)
            {
                var item = new Dictionary<string, JSONObject>();
                item.Add("id", new JSONObject(diagramMapping.DiagramElementID));
                //Create JSON Objects for all props that we have to retrieve.
                string[] sprops = {"label", "activation", "action", "resources", "capacity", "overflow"};
                List<JSONObject> props = new List<JSONObject>();
                foreach (string sprop in sprops)
                    props.Add(JSONObject.CreateStringObject(sprop));
                //Add props field.
                item.Add("props", new JSONObject(props.ToArray()));

                keys[i++] = new JSONObject(item);
            }

            //Finalize request by adding all top level items.
            initRequest.Add(SyncMsgs.JK_AUTH_DIAGRAM_TOKEN, JSONObject.CreateStringObject(diagramToken));
            //Wrapping the keys Array inside a JSON Object.
            initRequest.Add(SyncMsgs.JK_INIT_MACHINATIONS_IDS, new JSONObject(keys));

            _socket.Emit(SyncMsgs.SEND_GAME_INIT, new JSONObject(initRequest));
        }

        private void OnSocketOpen (SocketIOEvent e)
        {
            Debug.Log("[SocketIO] Open received: " + e.name + " " + e.data);
        }

        /// <summary>
        /// The Machinations Back-end has answered.
        /// </summary>
        /// <param name="e">Contains Init Data.</param>
        private void OnGameInitResponse (SocketIOEvent e)
        {
            Debug.Log("MGL [Hash:" + Instance.GetHashCode() + "] Received OnGameInitResponse DATA: " + e.data);

            //The answer from the back-end may contain multiple payloads.
            foreach (string payloadKey in e.data.keys)
                //For now, only interested in the "SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST" payload.
                if (payloadKey == SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST)
                    UpdateWithValuesFromMachinations(e.data[SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST].list);

            //Decrease number of responses pending from the back-end.
            _pendingResponses--;
            //When reaching 0, initialization is considered complete.
            if (_pendingResponses == 0)
                IsInitialized = true;
        }

        /// <summary>
        /// Occurs when the game has received an update from Machinations because some Diagram elements were changed.
        /// </summary>
        /// <param name="e">Contains Init Data.</param>
        private void OnDiagramElementsUpdated (SocketIOEvent e)
        {
            //The answer from the back-end may contain multiple payloads.
            foreach (string payloadKey in e.data.keys)
                //For now, only interested in the "SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST" payload.
                if (payloadKey == SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST)
                    UpdateWithValuesFromMachinations(e.data[SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST].list, true);
        }

        /// <summary>
        /// The Machinations Back-end has answered.
        /// </summary>
        /// <param name="e">Contains Init Data.</param>
        private void OnAuthSuccess (SocketIOEvent e)
        {
            Debug.Log("Game Auth Request Result: " + e.data);
            _pendingResponses--;
            //Initialization complete.
            if (_pendingResponses == 0)
                IsAuthenticated = true;
        }

        private void OnAuthDeny (SocketIOEvent e)
        {
            Debug.Log("Game Auth Request Failure: " + e.data);
            FailedToConnect();
        }

        private void OnSocketError (SocketIOEvent e)
        {
            Debug.Log("[SocketIO] !!!! Error received: " + e.name + " DATA: " + e.data + " ");
            FailedToConnect();
        }

        private void OnSocketClose (SocketIOEvent e)
        {
            Debug.Log("[SocketIO] !!!! Close received: " + e.name + " DATA:" + e.data);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a <see cref="MachinationsGameObjectManifest"/> to make sure that during Initialization, the MGL
        /// (aka <see cref="MachinationsGameLayer"/> retrieves all the Manifest's necessary data so that
        /// any Game Objects that use this Manifest can query the MGL for the needed values.
        /// </summary>
        static public void DeclareManifest (MachinationsGameObjectManifest manifest)
        {
            Debug.Log("MGL DeclareManifest: " + manifest);
            //Add all of this Manifest's targets to the list that we will have to Initialize & monitor.
            AddTargets(manifest.GetMachinationsDiagramTargets());
        }

        /// <summary>
        /// Registers a <see cref="IMachinationsScriptableObject"/> along with its Manifest.
        /// This is used to make sure that all Game Objects are ready for use after MGL Initialization.
        /// </summary>
        /// <param name="imso">The IMachinationsScriptableObject to add.</param>
        /// <param name="manifest">Its <see cref="MachinationsGameObjectManifest"/>.</param>
        static public void EnrollScriptableObject (IMachinationsScriptableObject imso,
            MachinationsGameObjectManifest manifest)
        {
            Debug.Log("MGL EnrollScriptableObject: " + manifest);
            DeclareManifest(manifest);
            if (!_scriptableObjects.ContainsKey(imso))
                _scriptableObjects[imso] = new EnrolledScriptableObject {MScriptableObject = imso, Manifest = manifest};
        }

        /// <summary>
        /// Registers a MachinationsGameObject that the Game Layer can keep track of.
        /// </summary>
        /// <param name="machinationsGameObject"></param>
        static public void EnrollGameObject (MachinationsGameObject machinationsGameObject)
        {
            _gameObjects.Add(machinationsGameObject);
            if (machinationsGameObject is MachinationsGameAwareObject gameAwareObject)
                _gameAwareObjects.Add(gameAwareObject);
            //If the MGL was already initialized OR is running in offline mode,
            //notifying this object that it can retrieve whatever information it needs from MGL.
            if (IsInitialized || IsInOfflineMode)
                machinationsGameObject.MGLReady();
        }

        /// <summary>
        /// Creates an <see cref="MachinationsUP.Integration.Elements.ElementBase"/>.
        /// </summary>
        /// <param name="elementBinder">The <see cref="MachinationsUP.Integration.Binder.ElementBinder"/> for which
        /// the <see cref="MachinationsUP.Integration.Elements.ElementBase"/> is to be created.</param>
        /// <param name="statesAssociation">OPTIONAL. The StatesAssociation for which the ElementBase is to be created.
        /// If this is not provided, the default value of NULL means that the ElementBase will use "N/A" as Title
        /// in the <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/> Init Request.</param>
        public ElementBase CreateElement (ElementBinder elementBinder, StatesAssociation statesAssociation = null)
        {
            ElementBase sourceElement = FindSourceElement(elementBinder, statesAssociation);
            //Not found elements are accepted in Offline Mode.
            if (sourceElement == null)
            {
                if (isInOfflineMode) return null;
                throw new Exception("MGL.CreateElement: Unhandled null Template Element.");
            }

            //Initialize the ElementBase by cloning it from the sourceElement.
            var newElement = sourceElement.Clone();

            Debug.Log("MGL.CreateValue complete for machinationsUniqueID '" +
                      GetMachinationsUniqueID(elementBinder, statesAssociation) + "'.");

            return newElement;
        }

        /// <summary>
        /// Returns the Source <see cref="MachinationsUP.Integration.Elements.ElementBase"/> found at the requested DiagramMapping.
        /// If Offline & caching active, will return the value previously loaded from the Cache.
        /// If Offline & caching inactive, returns the DefaultElementBase, if any was defined.
        /// Throws if cannot find any Source ElementBase.
        /// </summary>
        /// <param name="diagramMapping"><see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/> whose
        /// <see cref="MachinationsUP.Integration.Elements.ElementBase"/> to return.</param>
        /// <returns></returns>
        static public ElementBase GetSourceElementBase (DiagramMapping diagramMapping)
        {
            if (_sourceElements.ContainsKey(diagramMapping) && _sourceElements[diagramMapping] != null)
                return _sourceElements[diagramMapping];
            //When Offline, we may return the Default Element Base.
            if (IsInOfflineMode && diagramMapping.DefaultElementBase != null)
            {
                Debug.Log("MGL Returning DefaultElementBase for " + diagramMapping);
                return diagramMapping.DefaultElementBase;
            }

            throw new Exception("MGL.GetSourceElementBase: cannot find any Source Element Base for " + diagramMapping);
        }

        #endregion

        #region Properties

        /// <summary>
        /// TRUE when all Init-related tasks have been completed.
        /// </summary>
        static private bool IsAuthenticated { set; get; }

        static private bool isInitialized;

        /// <summary>
        /// TRUE when all Init-related tasks have been completed.
        /// </summary>
        static public bool IsInitialized
        {
            set
            {
                isInitialized = value;
                if (value)
                {
                    Debug.Log("MachinationsGameLayer Initialization Complete!");
                    NotifyAboutMGLInitComplete();
                }
            }
            get => isInitialized;
        }

        static private bool isInOfflineMode;

        /// <summary>
        /// MGL is running in offline mode.
        /// </summary>
        static public bool IsInOfflineMode
        {
            set
            {
                isInOfflineMode = value;
                if (value)
                {
                    Debug.Log("MachinationsGameLayer is now in Offline Mode!");
                    NotifyAboutMGLInitComplete(isInOfflineMode);
                }
            }
            get => isInOfflineMode;
        }

        /// <summary>
        /// Returns the current Game State, if any <see cref="IGameLifecycleProvider"/> is avaialble.
        /// </summary>
        /// <returns></returns>
        static public GameStates GetGameState ()
        {
            if (Instance._gameLifecycleProvider == null)
                throw new Exception("MGL no IGameLifecycleProvider available.");
            return Instance._gameLifecycleProvider.GetGameState();
        }

        /// <summary>
        /// Returns if the MGL has any cache loaded.
        /// </summary>
        static public bool HasCache => !string.IsNullOrEmpty(Instance.cacheDirectoryName) && _cache != null;

        #endregion

    }
}