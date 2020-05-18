import React, { useState, useContext } from "react";
import { Context } from "./data/Context";
import { NavLink } from "react-router-dom";
import { Navbar, Nav, FormControl, Button, InputGroup } from "react-bootstrap";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import {
    faSearch, faUserPlus, faSignOutAlt,
    faUserAlt, faUsers
} from "@fortawesome/free-solid-svg-icons";
import validator from "./utils/validator";
import LocalizedStrings from "react-localization";
const text = new LocalizedStrings({
    en: {
        Search: "Search",
        Lobby: "Lobby",
        Account: "Settings",
        My_Room: "My Room",
        Register: "Register",
        Log_Out: "Log Out"
    },
    ru: {
        Search: "Поиск",
        Lobby: "Лобби",
        Account: "Настройки",
        My_Room: "Моя Комната",
        Register: "Регистрация",
        Log_Out: "Выйти"
    }
})

export function TopMenu(props) {
    const context = useContext(Context);
    const [navExpanded, setExpanded] = useState(false);
    const toggleNav = expanded => setExpanded(expanded);
    const [page, setPage] = useState(1);
    let val, error;
    let refresh = false;
    if (!props.location.search) val = "";
    else if (props.location.search.startsWith("?q=")) {
        val = props.location.search.substring(3).replace(/_/g, ' ');
        if (val.length === 0) refresh = true;
        else error = validator.groupname(val, context.lang, true);
    } else refresh = true;
    if (refresh) {
        let newlocation = {};
        Object.assign(newlocation, props.location);
        newlocation.search = "";
        props.history.replace(newlocation);
    }
    const [search, setSearch] = useState(val);
    const [searchError, setSearchError] = useState(error);
    if (refresh) return null;
    const searchChanged = ev => {
        setSearch(ev.target.value);
        setSearchError(validator.groupname(ev.target.value, context.lang, true));
    }
    const applySearch = () => {
        if (search === val) return;
        if (searchError) alert(searchError);
        else if (!search) props.history.push("/lobby/1");
        else props.history.push("/lobby/1?q=" + search.replace(/\s/g, '_'));
    }
    if (props.location.pathname.startsWith("/lobby/")) {
        let num = Number(props.location.pathname.substring(7));
        if (isNaN(num)) {
            props.history.replace("/lobby/1");
        } else if (page !== num) setPage(num);
    }
    text.setLanguage(context.lang);
    return <Navbar bg="dark" variant="dark" expand="md" sticky="top"
        expanded={navExpanded} onToggle={toggleNav} style={{ userSelect: "none" }} >
        <Navbar.Brand>Rooms</Navbar.Brand>
        <InputGroup style={{ width: "55%", maxWidth: "245px" }} className="m-auto">
            <FormControl autoComplete="off" autoCorrect="off" autoCapitalize="off" spellCheck="false"
                placeholder={text.Search} value={search} onChange={searchChanged}
                className={`${searchError ? "text-danger border border-danger" : ""}`}
                onKeyPress={ev => { if (ev.which === 13) applySearch() }} />
            <InputGroup.Append>
                <Button className="ml-2" type="submit" variant="outline-light"
                    onClick={applySearch}><FontAwesomeIcon icon={faSearch} /></Button>
            </InputGroup.Append>
        </InputGroup>
        <Navbar.Toggle />
        <Navbar.Collapse>
            <Nav className="ml-auto">
                <Nav.Link className="m-auto" as={NavLink} to={`/lobby/${page}${search ? "?q=" + search.replace(/\s/g, '_') : ""}`} activeClassName="active">
                    <FontAwesomeIcon icon={faSearch} /> {text.Lobby}</Nav.Link>
                {
                    context.registered
                        ? <>
                            <Nav.Link className="m-auto" as={NavLink} to="/account" activeClassName="active">
                                <FontAwesomeIcon icon={faUserAlt} /> {text.Account}</Nav.Link>
                            <Nav.Link className="m-auto" as={NavLink} to="/myroom" activeClassName="active">
                                <FontAwesomeIcon icon={faUsers} /> {text.My_Room}</Nav.Link>
                        </>
                        : <Nav.Link className="m-auto" as={NavLink} to="/signin/user/register" activeClassName="active">
                            <FontAwesomeIcon icon={faUserPlus} /> {text.Register}</Nav.Link>
                }
                <Nav.Link onClick={ev => { ev.preventDefault(); context.signOut(); }} className="m-auto">
                    <FontAwesomeIcon icon={faSignOutAlt} /> {text.Log_Out}</Nav.Link>
            </Nav>
        </Navbar.Collapse>
    </Navbar>
}