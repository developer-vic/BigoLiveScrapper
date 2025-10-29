namespace BigoLiveScrapper.Data
{
    /// <summary>
    /// Constants for Bigo Live app automation selectors
    /// </summary>
    public class BigoLiveSConstants
    {
        // Package name
        public static readonly string PACKAGE_NAME = VConstants.BIGO_LIVE_APP_PACKAGE;

        // Resource IDs
        public static readonly string SEARCH_BUTTON_ID = "sg.bigo.live:id/iv_search";
        public static readonly string SEARCH_INPUT_ID = "sg.bigo.live:id/searchInput";
        public static readonly string SEARCH_CONFIRM_ID = "sg.bigo.live:id/searchOrCancel";
        public static readonly string SEARCH_RESULT_ID = "sg.bigo.live:id/searchOptimizeHotId";
        public static readonly string CONTRIB_ENTRY_ID = "sg.bigo.live:id/fl_contrib_entry";
        public static readonly string CONTRIB_TEXT_ID = "sg.bigo.live:id/tv_contribute";
        public static readonly string TAB_TITLE_ID = "sg.bigo.live:id/uiTabTitle";
        public static readonly string USER_NAME_ID = "sg.bigo.live:id/tv_name";
        public static readonly string CONTRIBUTION_AMOUNT_ID = "sg.bigo.live:id/tv_contribution";
        public static readonly string USER_LEVEL_ID = "sg.bigo.live:id/tv_user_level";
        public static readonly string BIGO_ID_ID = "sg.bigo.live:id/tv_bigo_id";
 
        // Timeouts
        public static readonly int DEFAULT_TIMEOUT = 10000;
        public static readonly int SHORT_TIMEOUT = 5000;
        public static readonly int LONG_TIMEOUT = 15000;
    }
}

