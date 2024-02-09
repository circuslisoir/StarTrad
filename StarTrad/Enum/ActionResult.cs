namespace StarTrad.Enum
{
	public enum ActionResult
    {
        Successful,  // Action was completed successfuly
        Failure,     // Action could not be completed
        Aborted,     // Action was aborted for a good reason
        UserCanceled // User canceled the action themselves
    }
}
