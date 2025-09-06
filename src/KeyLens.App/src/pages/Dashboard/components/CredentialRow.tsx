import { DateTime } from "luxon";
import type { CredentialRecord } from "../../../api";

export const CredentialRow: React.FC<CredentialRowProps> = ({ credential }) => {
    
    const expiresOn = credential.expiresOn ? DateTime.fromISO(credential.expiresOn) : null;

    const expiresOnText = expiresOn ? expiresOn.toLocaleString({ weekday: 'short', month: 'short', day: '2-digit', year: '2-digit' }) : '-';

    const expiresInText = expiresOn ? expiresOn.toRelative({ style: 'long' }) : '-';

    return (
        <tr key={credential.credentialId}>
            <td>{credential.credentialUri ? <a href={credential.credentialUri} target="creds">{credential.name}</a> : credential.name }</td>
            <td>{credential.kind}</td>
            <td>{expiresOnText}</td>
            <td>{expiresInText}</td>
            <td>{credential.notBefore ? DateTime.fromISO(credential.notBefore!).toLocaleString({ weekday: 'short', month: 'short', day: '2-digit', year: '2-digit' }) : '-'}</td>
            <td>{credential.provider}</td>
            <td>{credential.container}</td>
        </tr>
    );
};

export interface CredentialRowProps {
    credential: CredentialRecord;
}