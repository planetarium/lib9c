namespace Nekoyume.Model.Mail
{
    public interface IMail
    {
        void Read(CombinationMail mail);
        void Read(SellCancelMail mail);
        void Read(BuyerMail buyerMail);
        void Read(SellerMail sellerMail);
        void Read(ItemEnhanceMail itemEnhanceMail);
        void Read(DailyRewardMail dailyRewardMail);
        void Read(MonsterCollectionMail monsterCollectionMail);
        void Read(OrderExpirationMail orderExpirationMail);
        void Read(CancelOrderMail cancelOrderMail);
        void Read(OrderBuyerMail orderBuyerMail);
        void Read(OrderSellerMail orderSellerMail);
        void Read(GrindingMail grindingMail);
        void Read(MaterialCraftMail materialCraftMail);
        void Read(ProductBuyerMail productBuyerMail);
        void Read(ProductSellerMail productSellerMail);
        void Read(ProductCancelMail productCancelMail);
        void Read(UnloadFromMyGaragesRecipientMail unloadFromMyGaragesRecipientMail);
        void Read(ClaimItemsMail claimItemsMail);
        void Read(AdventureBossRaffleWinnerMail adventureBossRaffleWinnerMail);
        void Read(CustomCraftMail customCraftMail);
        void Read(PatrolRewardMail patrolRewardMail);
    }
}
