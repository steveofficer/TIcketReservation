module LedgerService.Serializer
open LedgerService.Types
open MongoDB.Bson

type LedgerTransactionSerializer() = 
    inherit Serialization.Serializers.SerializerBase<TransactionDetails>()
    override this.Serialize(context, args, value) = 
        let writer = context.Writer
        writer.WriteStartDocument()
        
        match value with
        | Quotation q -> 
            let serializeTicket (t : TicketInfo) = 
                writer.WriteStartDocument()
                writer.WriteName("TicketTypeId")
                writer.WriteString(t.TicketTypeId)
                writer.WriteName("PriceEach")
                writer.WriteDouble((float)t.PriceEach)
                writer.WriteName("Quantity")
                writer.WriteInt32((int32)t.Quantity)
                writer.WriteEndDocument()
            writer.WriteName("_t")
            writer.WriteString("Quotation")
            writer.WriteName("PricesQuotedAt")
            writer.WriteDateTime(BsonDateTime(q.PricesQuotedAt).MillisecondsSinceEpoch)
            writer.WriteName("TotalPrice")
            writer.WriteDouble((float)q.TotalPrice)
            writer.WriteName("Tickets")
            writer.WriteStartArray()
            q.Tickets |> Array.iter serializeTicket
            writer.WriteEndArray()
            ()
        | Cancellation c -> 
            let serializeTicket (t : CancelledTicket) = 
                writer.WriteStartDocument()
                writer.WriteName("TicketTypeId")
                writer.WriteString(t.TicketTypeId)
                writer.WriteName("TicketId")
                writer.WriteString(t.TicketId)
                writer.WriteName("Price")
                writer.WriteDouble((float)t.Price)
                writer.WriteEndDocument()
            writer.WriteName("_t")
            writer.WriteString("Cancellation")
            writer.WriteName("Tickets")
            writer.WriteStartArray()
            c.Tickets |> Array.iter serializeTicket
            writer.WriteEndArray()
            ()
        | Allocation a -> 
            let serializeTicket (t : AllocatedTicket) = 
                writer.WriteStartDocument()
                writer.WriteName("TicketTypeId")
                writer.WriteString(t.TicketTypeId)
                writer.WriteName("TicketId")
                writer.WriteString(t.TicketId)
                writer.WriteName("Price")
                writer.WriteDouble((float)t.Price)
                writer.WriteEndDocument()
            writer.WriteName("_t")
            writer.WriteString("Allocation")
            writer.WriteName("Tickets")
            writer.WriteStartArray()
            a.Tickets |> Array.iter serializeTicket
            writer.WriteEndArray()
            writer.WriteName("TotalPrice")
            writer.WriteDouble((float)a.TotalPrice)
            ()
        writer.WriteEndDocument()

