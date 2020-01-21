
var camelCaseTokenizer = function (builder) {

  var pipelineFunction = function (token) {
    var previous = '';
    // split camelCaseString to on each word and combined words
    // e.g. camelCaseTokenizer -> ['camel', 'case', 'camelcase', 'tokenizer', 'camelcasetokenizer']
    var tokenStrings = token.toString().trim().split(/[\s\-]+|(?=[A-Z])/).reduce(function(acc, cur) {
      var current = cur.toLowerCase();
      if (acc.length === 0) {
        previous = current;
        return acc.concat(current);
      }
      previous = previous.concat(current);
      return acc.concat([current, previous]);
    }, []);

    // return token for each string
    // will copy any metadata on input token
    return tokenStrings.map(function(tokenString) {
      return token.clone(function(str) {
        return tokenString;
      })
    });
  }

  lunr.Pipeline.registerFunction(pipelineFunction, 'camelCaseTokenizer')

  builder.pipeline.before(lunr.stemmer, pipelineFunction)
}
var searchModule = function() {
    var documents = [];
    var idMap = [];
    function a(a,b) { 
        documents.push(a);
        idMap.push(b); 
    }

    a(
        {
            id:0,
            title:"TicketContract Venue",
            content:"TicketContract Venue",
            description:'',
            tags:''
        },
        {
            url:'/api/global/Venue',
            title:"TicketContract_1_0_0.Venue",
            description:""
        }
    );
    a(
        {
            id:1,
            title:"TicketContract Seat",
            content:"TicketContract Seat",
            description:'',
            tags:''
        },
        {
            url:'/api/global/Seat',
            title:"TicketContract_1_0_0.Seat",
            description:""
        }
    );
    a(
        {
            id:2,
            title:"TicketContract",
            content:"TicketContract",
            description:'',
            tags:''
        },
        {
            url:'/api/global/TicketContract_1_0_0',
            title:"TicketContract_1_0_0",
            description:""
        }
    );
    a(
        {
            id:3,
            title:"TicketContract IdentityVerificationPolicy",
            content:"TicketContract IdentityVerificationPolicy",
            description:'',
            tags:''
        },
        {
            url:'/api/global/IdentityVerificationPolicy',
            title:"TicketContract_1_0_0.IdentityVerificationPolicy",
            description:""
        }
    );
    a(
        {
            id:4,
            title:"TicketContract Show",
            content:"TicketContract Show",
            description:'',
            tags:''
        },
        {
            url:'/api/global/Show',
            title:"TicketContract_1_0_0.Show",
            description:""
        }
    );
    a(
        {
            id:5,
            title:"TicketContract TicketReleaseFee",
            content:"TicketContract TicketReleaseFee",
            description:'',
            tags:''
        },
        {
            url:'/api/global/TicketReleaseFee',
            title:"TicketContract_1_0_0.TicketReleaseFee",
            description:""
        }
    );
    a(
        {
            id:6,
            title:"TicketContract Ticket",
            content:"TicketContract Ticket",
            description:'',
            tags:''
        },
        {
            url:'/api/global/Ticket',
            title:"TicketContract_1_0_0.Ticket",
            description:""
        }
    );
    a(
        {
            id:7,
            title:"TicketContract NoRefundBlocks",
            content:"TicketContract NoRefundBlocks",
            description:'',
            tags:''
        },
        {
            url:'/api/global/NoRefundBlocks',
            title:"TicketContract_1_0_0.NoRefundBlocks",
            description:""
        }
    );
    var idx = lunr(function() {
        this.field('title');
        this.field('content');
        this.field('description');
        this.field('tags');
        this.ref('id');
        this.use(camelCaseTokenizer);

        this.pipeline.remove(lunr.stopWordFilter);
        this.pipeline.remove(lunr.stemmer);
        documents.forEach(function (doc) { this.add(doc) }, this)
    });

    return {
        search: function(q) {
            return idx.search(q).map(function(i) {
                return idMap[i.ref];
            });
        }
    };
}();
