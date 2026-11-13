import { createContext, useContext } from 'react';
export const WorkspaceContext = createContext({ currency: '', spaceName: '', canEdit: false, canAudit: false });
export const useWorkspace = () => useContext(WorkspaceContext);
