using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CardBattleArtDemo : MonoBehaviour
{
    CardBattleGame game;
    Sprite[] sprites = new Sprite[32];
    Image hero, monster;
    Text heading, stats, enemyName, intent, log, build, result;
    Button[] actions = new Button[4];
    Text[] labels = new Text[4];
    RectTransform choices;
    Font font;
    GameObject canvasObject;
    bool busy;
    readonly Color dark = new Color(.06f,.09f,.13f);
    readonly Color panel = new Color(.12f,.18f,.23f);
    readonly Color paper = new Color(.94f,.90f,.82f);
    readonly Color gold = new Color(.86f,.72f,.45f);

    void Start()
    {
        var atlas = Resources.Load<Texture2D>("CardBattleAtlas");
        if (atlas == null) { Debug.LogError("找不到 Assets/Resources/CardBattleAtlas.png"); enabled = false; return; }
        var original = GetComponent<CardBattleUnityDemo>();
        if (original != null) original.enabled = false;
        font = Resources.Load<Font>("CardBattleFont");
        if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft JhengHei", "Microsoft YaHei", "Arial" }, 22);
        float w = atlas.width / 8f, h = atlas.height / 4f;
        for (int i=0; i<32; i++) sprites[i] = Sprite.Create(atlas, new Rect(i%8*w,(3-i/8)*h,w,h),new Vector2(.5f,.5f),100);
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            var es = new GameObject("Battle Events", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }
        canvasObject = new GameObject("Battle Art UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scale = canvasObject.GetComponent<CanvasScaler>();
        scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1280,800);
        scale.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var bg = Box(canvasObject.transform,new Rect(0,0,1280,800),dark);
        Label(bg,new Rect(20,15,900,50),"五關試煉 / FIVE TRIALS",32,gold);
        MakeButton(bg,new Rect(1060,20,195,45),"重新開始",Restart);
        var arena = Box(bg,new Rect(20,80,770,360),panel);
        heading = Label(arena,new Rect(15,5,370,45),"",24,gold);
        enemyName = Label(arena,new Rect(395,5,365,45),"",22,paper);
        hero = Box(arena,new Rect(15,50,310,255),Color.white).GetComponent<Image>();
        monster = Box(arena,new Rect(430,50,310,255),Color.white).GetComponent<Image>();
        hero.preserveAspect = monster.preserveAspect = true;
        Label(arena,new Rect(345,140,75,60),"VS",28,gold);
        intent = Label(arena,new Rect(15,305,740,50),"",21,paper);
        stats = Label(bg,new Rect(25,445,760,45),"",23,paper);
        for(int i=0;i<4;i++)
        {
            int n=i;
            actions[i]=MakeButton(bg,new Rect(20+i*195,500,180,100),"",()=>Act((PlayerAction)n));
            labels[i]=actions[i].GetComponentInChildren<Text>();
        }
        result=Label(bg,new Rect(25,610,760,55),"",23,gold);
        build=Label(bg,new Rect(25,675,760,100),"",20,paper);
        var logs=Box(bg,new Rect(810,80,450,695),panel);
        Label(logs,new Rect(15,5,420,45),"戰鬥紀錄",25,gold);
        log=Label(logs,new Rect(15,60,420,620),"",18,paper);
        log.alignment=TextAnchor.UpperLeft;
        choices=Box(bg,new Rect(20,440,770,335),dark);
        game=new CardBattleGame(); Restart();
    }

    RectTransform Box(Transform parent,Rect r,Color color)
    {
        var go=new GameObject("Panel",typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
        var t=(RectTransform)go.transform;
        t.anchorMin=t.anchorMax=t.pivot=new Vector2(0,1);
        t.anchoredPosition=new Vector2(r.x,-r.y); t.sizeDelta=new Vector2(r.width,r.height);
        go.GetComponent<Image>().color=color; return t;
    }
    Text Label(Transform parent,Rect r,string value,int size,Color color)
    {
        var go=new GameObject("Text",typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
        var t=(RectTransform)go.transform;
        t.anchorMin=t.anchorMax=t.pivot=new Vector2(0,1);
        t.anchoredPosition=new Vector2(r.x,-r.y); t.sizeDelta=new Vector2(r.width,r.height);
        var text=go.GetComponent<Text>();
        text.font=font; text.fontSize=size; text.color=color; text.text=value;
        text.alignment=TextAnchor.MiddleLeft; text.raycastTarget=false; text.supportRichText=false;
        text.horizontalOverflow=HorizontalWrapMode.Wrap;
        return text;
    }
    Button MakeButton(Transform parent,Rect r,string title,UnityEngine.Events.UnityAction click)
    {
        var t=Box(parent,r,panel); var b=t.gameObject.AddComponent<Button>();
        b.targetGraphic=t.GetComponent<Image>(); b.onClick.AddListener(click);
        var text=Label(t,new Rect(10,3,r.width-20,r.height-6),title,20,paper);
        text.alignment=TextAnchor.MiddleCenter;
        return b;
    }
    int EnemyIndex()
    {
        string[] names={"哥布林","獸人","幽靈","史萊姆","哥布林王","巨魔","食人魔酋長","遠古遺跡守衛"};
        int i=System.Array.IndexOf(names,game.EnemyName); return i<0?0:i;
    }
    void Restart()
    {
        if(game==null)return;
        StopAllCoroutines(); busy=false; game.StartNewRun(); result.text="";
        hero.sprite=sprites[16]; hero.color=monster.color=Color.white;
        monster.rectTransform.localScale=Vector3.one; Refresh();
    }
    void Refresh()
    {
        heading.text="第 "+game.Stage+" / 5 關"+(game.Stage==5?" · BOSS":"");
        enemyName.text=game.EnemyName+"  HP "+game.EnemyHp+"/"+game.EnemyMaxHp;
        stats.text="生命 "+game.PlayerHp+" / 100     魔力 "+game.PlayerMana+" / "+game.PlayerMaxMana;
        intent.text=busy?"交鋒中…":"敵方動向："+game.CurrentIntentText;
        monster.sprite=sprites[EnemyIndex()];
        labels[0].text="普攻\n"+game.PlayerAttackDamage+" 傷害\n+"+game.PlayerAttackManaGain+" 魔力";
        labels[1].text="暗器\n"+game.PlayerDartDamage+" 傷害\n消耗 "+game.PlayerDartCost+" 魔力";
        labels[2].text="防禦\n一般減傷 "+game.PlayerDefenseReduction+"%\n大招 / 重擊減傷 50%";
        labels[3].text="大招\n"+game.PlayerUltimateDamage+" 傷害\n消耗 45 魔力";
        for(int i=0;i<4;i++)actions[i].interactable=!busy&&!game.IsResting&&!game.IsGameOver&&!game.HasWon&&game.CanUse((PlayerAction)i);
        build.text="流派："+game.BuildText+"\n暗器打斷普攻 · 防禦回魔 · 生命跨關保留";
        log.text="";
        for(int i=System.Math.Max(0,game.Log.Count-8);i<game.Log.Count;i++)log.text+=game.Log[i]+"\n\n";
        foreach(Transform child in choices){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        choices.gameObject.SetActive(!busy&&(game.IsResting||game.IsGameOver||game.HasWon));
        if(busy)return;
        if(game.IsResting)
        {
            Label(choices,new Rect(15,0,740,50),"整備：已回復 10 HP，請選擇一項獎勵",23,gold);
            MakeButton(choices,new Rect(15,55,740,50),"額外回復 15 HP",()=>{game.ChooseExtraHeal();result.text="";Refresh();});
            for(int i=0;i<game.UpgradeOffers.Count;i++)
            {
                var offer=game.UpgradeOffers[i];
                MakeButton(choices,new Rect(15,115+i*55,740,50),offer.Label,()=>{game.ChooseUpgrade(offer.Id);result.text="";Refresh();});
            }
        }
        else if(game.IsGameOver||game.HasWon)
        {
            Label(choices,new Rect(30,30,700,100),game.HasWon?"五關制霸！恭喜通關！":"試煉結束：到達第 "+game.Stage+" 關",30,gold);
            MakeButton(choices,new Rect(30,160,700,65),"再挑戰一輪",Restart);
        }
    }
    void Act(PlayerAction action)
    {
        if(busy||game.IsResting||game.IsGameOver||game.HasWon||!game.CanUse(action))return;
        StartCoroutine(Turn(action));
    }
    IEnumerator Turn(PlayerAction action)
    {
        busy=true; Refresh(); result.text="";
        int[][] poses={new[]{17,18,19},new[]{20,21,22},new[]{23,24,25},new[]{26,27,28}};
        int[] pose=poses[(int)action]; int e=EnemyIndex();
        hero.sprite=sprites[pose[0]]; yield return new WaitForSecondsRealtime(.15f);
        hero.sprite=sprites[pose[1]];
        int hp=game.PlayerHp,ehp=game.EnemyHp; game.TakeTurn(action);
        int hurt=hp-game.PlayerHp,damage=System.Math.Max(0,ehp-game.EnemyHp);
        result.text="敵方 −"+damage+" HP     你 −"+hurt+" HP";
        for(int i=0;i<6;i++)
        {
            bool flash=i%2==0;
            if(damage>0){monster.sprite=sprites[flash?e+8:e];monster.color=flash?new Color(1,.55f,.55f):Color.white;monster.rectTransform.localScale=Vector3.one*(flash?.94f:1);}
            if(hurt>0){hero.color=flash?new Color(1,.55f,.55f):Color.white;if(i>=2)hero.sprite=sprites[flash?29:30];}
            yield return new WaitForSecondsRealtime(.07f);
        }
        hero.sprite=sprites[pose[2]]; yield return new WaitForSecondsRealtime(.15f);
        hero.sprite=sprites[16]; hero.color=monster.color=Color.white;
        monster.rectTransform.localScale=Vector3.one; busy=false; Refresh();
    }
    void OnDestroy()
    {
        if(canvasObject!=null)Destroy(canvasObject);
        foreach(var sprite in sprites)if(sprite!=null)Destroy(sprite);

    }
}
