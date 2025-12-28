/**
 * INVALID: Component that uses direct fetch instead of API service.
 * This violates project conventions and should be detected.
 */

import { useState, useEffect } from 'react';

interface Person {
  idKey: string;
  firstName: string;
  lastName: string;
}

/**
 * VIOLATION: Component with direct fetch call instead of using API service layer.
 */
export function DirectFetchComponent() {
  const [people, setPeople] = useState<Person[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // VIOLATION: Direct fetch instead of using peopleApi.searchPeople()
    fetch('/api/v1/people')
      .then(response => response.json())
      .then(data => {
        setPeople(data.data);
        setLoading(false);
      })
      .catch(error => {
        console.error('Error fetching people:', error);
        setLoading(false);
      });
  }, []);

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <div>
      <h1>People</h1>
      <ul>
        {people.map(person => (
          <li key={person.idKey}>
            {person.firstName} {person.lastName}
          </li>
        ))}
      </ul>
    </div>
  );
}

/**
 * VIOLATION: Another component with direct fetch and axios.
 */
export function DirectAxiosComponent({ idKey }: { idKey: string }) {
  const [person, setPerson] = useState<Person | null>(null);

  useEffect(() => {
    // VIOLATION: Direct axios call instead of using API service
    import('axios').then(({ default: axios }) => {
      axios.get(`/api/v1/people/${idKey}`)
        .then(response => setPerson(response.data.data))
        .catch(error => console.error(error));
    });
  }, [idKey]);

  if (!person) {
    return <div>Loading...</div>;
  }

  return <div>{person.firstName}</div>;
}

/**
 * VIOLATION: Component that doesn't use TanStack Query.
 */
export function NoQueryComponent() {
  const [data, setData] = useState(null);

  // VIOLATION: Manual state management instead of useQuery
  const loadData = async () => {
    const response = await fetch('/api/v1/people');
    const json = await response.json();
    setData(json.data);
  };

  return <button onClick={loadData}>Load</button>;
}
