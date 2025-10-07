using System;
using Bencodex.Types;
using Lib9c.Model.Market;
using Lib9c.Model.State;
using static Lib9c.SerializeKeys;


namespace Lib9c.Model.Mail
{
    [Serializable]
    public class ProductBuyerMail : Mail
    {
        public const string ProductKey = "p";
        public readonly Guid ProductId;
        public readonly Product Product;
        public ProductBuyerMail(long blockIndex, Guid id, long requiredBlockIndex, Guid productId, Product product) : base(blockIndex, id, requiredBlockIndex)
        {
            ProductId = productId;
            Product = product;
        }

        public ProductBuyerMail(Dictionary serialized) : base(serialized)
        {
            ProductId = serialized[ProductIdKey].ToGuid();
            if (serialized.ContainsKey(ProductKey))
            {
                Product = ProductFactory.DeserializeProduct((List) serialized[ProductKey]);
            }
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public override MailType MailType => MailType.Auction;

        protected override string TypeId => nameof(ProductBuyerMail);

        public override IValue Serialize()
        {
            var dict = ((Dictionary) base.Serialize())
                .Add(ProductIdKey, ProductId.Serialize());
            if (Product is not null)
            {
                dict = dict.Add(ProductKey, Product.Serialize());
            }

            return dict;

        }
    }
}
