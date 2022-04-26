using System;
using System.Collections.Generic;
using System.Linq;

namespace CQRS_and_EventSourcing
{
    internal class Program
    {
        //CQRS = command query responsibility segregation
        //CQS= command query separation

        //COMMAND

        public class PersonStroge
        {
            Dictionary<int, Person> people;
        }

        public class Person
        {
            public int UniqueId;
            public int age;
            EventBroker broker;

            public Person(EventBroker broker)
            {
                this.broker = broker;
                broker.Commands += BrokerOnCommands;
                broker.Queries += BrokeronQueries;
            }

            private void BrokeronQueries(object sender, Query query)
            {
                var ac = query as AgeQuery;
                if (ac != null && ac.Target == this)
                {
                    ac.Result = age;
                }
            }

            private void BrokerOnCommands(object sender, Command command)
            {
                var cac = command as ChangeAgeCommand;
                if (cac != null && cac.Target == this)
                {
                    if (cac.Register)
                        broker.AllEvents.Add(new AgeChangedEvent(this, age, cac.Age));
                    age = cac.Age;
                }
            }
            public bool CanVote => age >= 16;
        }
        public class EventBroker
        {
            //1. All events that happened.
            public IList<Event> AllEvents = new List<Event>();
            //2. Commands
            public event EventHandler<Command> Commands;
            //3. Query
            public event EventHandler<Query> Queries;

            public void Command(Command c)
            {
                Commands?.Invoke(this, c);
            }

            public T Query<T>(Query q)
            {
                Queries?.Invoke(this, q);
                return (T)q.Result;
            }

            public void UndoLast()
            {
                var e = AllEvents.LastOrDefault();
                var ac = e as AgeChangedEvent;
                if (ac != null)
                {
                    Command(new ChangeAgeCommand(ac.Target, ac.OldValue) { Register = false });
                    AllEvents.Remove(e);
                }
            }
        }
        public class Query
        {
            public object Result;
        }
        public class AgeQuery : Query
        {
            public Person Target;
        }
        public class Command : EventArgs

        {
            public bool Register = true;
        }
        public class ChangeAgeCommand : Command
        {
            public Person Target;
            //public int TargetId;
            public int Age;

            public ChangeAgeCommand(Person target, int age)
            {
                Target = target;
                Age = age;
            }
        }

        public class Event
        {
            //backtrack
        }
        public class AgeChangedEvent : Event
        {
            public Person Target;
            public int OldValue, NewValue;

            public AgeChangedEvent(Person target, int oldValue, int newValue)
            {
                Target = target;
                OldValue = oldValue;
                NewValue = newValue;
            }
            public override string ToString()
            {
                return $"Age changed from {OldValue} to {NewValue}";
            }
        }

        static void Main(string[] args)
        {
            var eb = new EventBroker();
            var p = new Person(eb);

            eb.Command(new ChangeAgeCommand(p, 123));

            foreach (var e in eb.AllEvents)
            {
                Console.WriteLine(e);
            }

            int age;

            age = eb.Query<int>(new AgeQuery { Target = p });
            Console.WriteLine(age);

            eb.UndoLast();

            foreach (var e in eb.AllEvents)
            {
                Console.WriteLine(e);
            }

            age = eb.Query<int>(new AgeQuery { Target = p });
            Console.WriteLine(age);

            Console.ReadKey();
        }
    }
}
