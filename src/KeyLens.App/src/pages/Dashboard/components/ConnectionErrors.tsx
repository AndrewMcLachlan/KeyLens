import type { ConnectionError } from "../../../api";

export const ConnectionErrors: React.FC<{ errors?: ConnectionError[] }> = ({ errors }) => {

    if (!errors?.length) return null;

    return (
        <section role="alert" className="connection-errors">
            <h2>Some connections could not be scanned</h2>
            <ul>
                {errors.map(e => (
                    <li key={e.connectionId}><strong>{e.connectionName}</strong>: {e.error}</li>
                ))}
            </ul>
        </section>
    );
}
