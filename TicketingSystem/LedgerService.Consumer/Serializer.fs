module Serializer
open Model
open MongoDB.Bson

type LedgerTransactionSerializer() = 
    inherit Serialization.Serializers.SerializerBase<TransactionDetails>()
    override this.Serialize(context, args, value) = 
        context.Writer.WriteStartDocument()
        
        match value with
        | Quotation q -> 
            let serializeTicket (t : TicketInfo) = 
                context.Writer.WriteStartDocument()
                context.Writer.WriteName("TicketTypeId")
                context.Writer.WriteString(t.TicketTypeId)
                context.Writer.WriteName("PriceEach")
                context.Writer.WriteDouble((float)t.PriceEach)
                context.Writer.WriteName("Quantity")
                context.Writer.WriteInt32((int32)t.Quantity)
                context.Writer.WriteEndDocument()
            context.Writer.WriteName("_t")
            context.Writer.WriteString("Quotation")
            context.Writer.WriteName("PricesQuotedAt")
            context.Writer.WriteDateTime(BsonDateTime(q.PricesQuotedAt).MillisecondsSinceEpoch)
            context.Writer.WriteName("TotalPrice")
            context.Writer.WriteDouble((float)q.TotalPrice)
            context.Writer.WriteName("Tickets")
            context.Writer.WriteStartArray()
            q.Tickets |> Array.iter serializeTicket
            context.Writer.WriteEndArray()
            ()
        | Cancellation c -> 
            context.Writer.WriteName("_t")
            context.Writer.WriteString("Cancellation")
            context.Writer.WriteStartArray()
            c.TicketIds |> Array.iter context.Writer.WriteString
            context.Writer.WriteEndArray()
            ()
        | Allocation a -> 
            let serializeTicket (t : AllocatedTicket) = 
                context.Writer.WriteStartDocument()
                context.Writer.WriteName("TicketTypeId")
                context.Writer.WriteString(t.TicketTypeId)
                context.Writer.WriteName("TicketId")
                context.Writer.WriteString(t.TicketId)
                context.Writer.WriteEndDocument()
            context.Writer.WriteName("_t")
            context.Writer.WriteString("Allocation")
            context.Writer.WriteStartArray()
            a.Tickets |> Array.iter serializeTicket
            context.Writer.WriteEndArray()
            ()
        context.Writer.WriteEndDocument()

