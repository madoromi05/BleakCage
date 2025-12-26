using UnityEngine;

public class HomeCommandButton : MonoBehaviour
{
    private HomeManager homeManager;

    // Manager‚©‚çŒÄ‚Î‚ê‚Ä•R‚Ã‚¯‚ðs‚¤
    public void Setup(HomeManager manager)
    {
        this.homeManager = manager;
    }

    public void OnClickStory() => homeManager?.OnClickStory();
    public void OnClickTutorial() => homeManager?.OnClickTutorial();
    public void OnClickOption() => homeManager?.OnClickOption();
    public void OnClickQuit() => homeManager?.OnClickQuit();
}