import { Page } from "@andrewmclachlan/moo-app";
import { useGetCredentials } from "./hooks/useGetCredentials";
import { SectionTable } from "@andrewmclachlan/moo-ds";
import { CredentialRow } from "./components/CredentialRow";


export const Dashboard = () => {

    const { data: credentials, isLoading, isError, error } = useGetCredentials();

    return (
        <Page title="Dashboard">
            <SectionTable header="Credentials">
                <thead>
                    <th>Name</th>
                    <th>Type</th>
                    <th>Expiry</th>
                    <th>Expires</th>
                    <th>Start</th>
                    <th>Provider</th>
                    <th>Source</th>
                </thead>
                <tbody>
                    {credentials?.map(c => (
                        <CredentialRow key={c.credentialId} credential={c} />
                    ))}
                    {isLoading && Array.from({ length: 10  }).map((_, i) => <tr key={i}><td colSpan={7}></td></tr>)}
                </tbody>
            </SectionTable>
        </Page>
    );
}