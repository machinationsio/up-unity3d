namespace MachinationsUP.SyncAPI
{
    static public class SyncMsgs
    {

        //Socket IO Event names.

        //Send.
        public const string SEND_API_AUTHORIZE = "api-authorize";
        public const string SEND_GAME_INIT = "game-init";
        public const string SEND_GAME_EVENT = "game-event";
        //Receive.
        public const string RECEIVE_OPEN_START = "open-start";
        public const string RECEIVE_OPEN = "open";
        public const string RECEIVE_ERROR = "error";
        public const string RECEIVE_API_ERROR = "api-error";
        public const string RECEIVE_AUTH_SUCCESS = "api-auth-success";
        public const string RECEIVE_AUTH_DENY = "api-auth-deny";
        public const string RECEIVE_GAME_INIT = "game-init";
        public const string RECEIVE_DIAGRAM_ELEMENTS_UPDATED = "diagram-elements-updated";
        public const string RECEIVE_CLOSE = "close";

        //JSON keys used for communication.

        //Auth request.
        public const string JK_AUTH_GAME_NAME = "gameName";
        public const string JK_AUTH_DIAGRAM_TOKEN = "diagramToken";
        //Init request.
        public const string JK_INIT_MACHINATIONS_IDS = "machinationsIDs";
        //Init response.
        public const string JK_DIAGRAM_ELEMENTS_LIST = "diagramElements";
        //Game Event.
        public const string JK_EVENT_GAME_OBJ_NAME = "gameObjName";
        public const string JK_EVENT_GAME_EVENT = "gameEvent";

        //Parameters received in JSON communcation.

        //Machinations Diagram Element Properties.
        public const string JP_DIAGRAM_ID = "id";
        public const string JP_DIAGRAM_ELEMENT_TYPE = "type";
        public const string JP_DIAGRAM_LABEL = "label";
        public const string JP_DIAGRAM_ACTIVATION = "activation";
        public const string JP_DIAGRAM_ACTION = "action";
        public const string JP_DIAGRAM_RESOURCES = "resources";
        public const string JP_DIAGRAM_CAPACITY = "capacity";
        public const string JP_DIAGRAM_OVERFLOW = "overflow";

    }
}
