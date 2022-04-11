using System;
using Demo2D.Frame;

namespace Demo2D
{
    [Serializable]
    public class CharacterStateInfo
    {
        /*有交互的数据,要显示到UI上的*/
        //血量
        public ReactiveProperty<int> hp = new ReactiveProperty<int>(0);
        //蓝量
        public ReactiveProperty<int> mp = new ReactiveProperty<int>(0);


        /*普通的数据*/
        //是否无敌
        public bool invulnerable;
        //无敌时间
        public float invulnerableTime;
        //击退抗性
        public int knbr;

        public CharacterStateInfo(int hp, int mp, int knbr, bool inv)
        {
            this.hp.Data = hp;
            this.mp.Data = mp;
            invulnerable = inv;
            this.knbr = knbr;
            invulnerableTime = 0.1f;
        }

        public CharacterStateInfo() : this(0, 0, 0, false)
        {

        }
        
    }
}